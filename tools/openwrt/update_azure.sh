#!/bin/sh

# Azure DNS update script for OpenWrt ddns-scripts
# Reads Azure credentials from /etc/ddns/azure.env and expects ddns core to
# provide the target IP via the __IP variable. The surrounding ddns runtime
# already sourced dynamic_dns_functions.sh, so we just reuse write_log here.

ENV_FILE="/etc/ddns/azure.env"

if [ ! -r "$ENV_FILE" ]; then
    write_log 3 "Azure env file missing at $ENV_FILE"
    return 1
fi

. "$ENV_FILE"

IP_VALUE="${__IP:-$1}"
if [ -z "$IP_VALUE" ]; then
    write_log 3 "Azure update called without IP value"
    return 1
fi

if ! command -v curl >/dev/null 2>&1; then
    write_log 3 "curl binary required for Azure DNS updates"
    return 1
fi

if ! command -v jsonfilter >/dev/null 2>&1; then
    write_log 3 "jsonfilter utility is required"
    return 1
fi

if [ -z "$AZURE_TENANT_ID" ] || [ -z "$AZURE_CLIENT_ID" ] || \
   [ -z "$AZURE_CLIENT_SECRET" ] || [ -z "$AZURE_SCOPE" ]; then
    write_log 3 "Azure OAuth configuration incomplete"
    return 1
fi

if [ -z "$AZURE_SUBSCRIPTION_ID" ] || [ -z "$AZURE_RESOURCE_GROUP" ] || \
   [ -z "$AZURE_DNS_ZONE" ] || [ -z "$AZURE_RECORD_SET" ]; then
    write_log 3 "Azure DNS target configuration incomplete"
    return 1
fi

TTL_VALUE=${AZURE_RECORD_TTL:-300}
API_VERSION=${AZURE_API_VERSION:-2018-05-01}

TOKEN_RESPONSE=$(curl -fsS -X POST \
    "https://login.microsoftonline.com/${AZURE_TENANT_ID}/oauth2/v2.0/token" \
    -H "Content-Type: application/x-www-form-urlencoded" \
    --data-urlencode "client_id=${AZURE_CLIENT_ID}" \
    --data-urlencode "client_secret=${AZURE_CLIENT_SECRET}" \
    --data-urlencode "scope=${AZURE_SCOPE}" \
    --data-urlencode "grant_type=client_credentials")
TOKEN_STATUS=$?

if [ $TOKEN_STATUS -ne 0 ]; then
    write_log 3 "Failed to obtain Azure access token"
    return 1
fi

ACCESS_TOKEN=$(echo "$TOKEN_RESPONSE" | jsonfilter -e '@.access_token')
if [ -z "$ACCESS_TOKEN" ]; then
    write_log 3 "Azure token parse failure"
    write_log 7 "Azure token payload: $TOKEN_RESPONSE"
    return 1
fi

API_URL="https://management.azure.com/subscriptions/${AZURE_SUBSCRIPTION_ID}/resourceGroups/${AZURE_RESOURCE_GROUP}/providers/Microsoft.Network/dnsZones/${AZURE_DNS_ZONE}/A/${AZURE_RECORD_SET}?api-version=${API_VERSION}"
REQUEST_BODY=$(printf '{"properties":{"TTL":%s,"ARecords":[{"ipv4Address":"%s"}]}}' "$TTL_VALUE" "$IP_VALUE")

UPDATE_RESPONSE=$(curl -fsS -X PUT "$API_URL" \
    -H "Authorization: Bearer ${ACCESS_TOKEN}" \
    -H "Content-Type: application/json" \
    --data "$REQUEST_BODY")
UPDATE_STATUS=$?

if [ $UPDATE_STATUS -ne 0 ]; then
    write_log 3 "Azure DNS update request failed"
    write_log 7 "Azure request body: $REQUEST_BODY"
    return 1
fi

ETAG=$(echo "$UPDATE_RESPONSE" | jsonfilter -e '@.etag')

if [ -z "$ETAG" ]; then
    write_log 4 "Azure DNS update response did not include etag"
else
    write_log 6 "Azure DNS update succeeded with etag $ETAG"
fi

return 0
