using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Hartonomous.Testing.Common.Seeds;

namespace Hartonomous.Testing.Common;

public static class TestData
{
    public static class Text
    {
        public const string SampleRelativePath = "text/sample.txt";
        public const string SampleContent = "hello autonomous world";
        public const string SampleSha256 = "d3794f37f231c39e6803f8a3a7ea6f94964e7edd6ade45c34b82bff31fafda14";
    }

    public static class Json
    {
        public static class Identity
        {
            public const string TenantsRelativePath = "json/tenants.seed.json";
            public const string TenantsSha256 = "0974be7563dc0fd379fbeab79b03f49c41c366284a8965025a7b321b89e2e472";

            public const string PrincipalsRelativePath = "json/principals.seed.json";
            public const string PrincipalsSha256 = "fc19b03073fafb093f6939635e93b0cab29fca40847d6698e68308ef490e0b62";

            public const string PoliciesRelativePath = "json/policies.seed.json";
            public const string PoliciesSha256 = "1a31c22d320e10e3be8e515966d8301c4c083de3be52149199a7f51119ca5150";

            public static IdentitySeedData Load()
            {
                var tenants = Deserialize<IReadOnlyList<TenantSeed>>(TenantsRelativePath);
                var principals = Deserialize<IReadOnlyList<PrincipalSeed>>(PrincipalsRelativePath);
                var policies = Deserialize<IReadOnlyList<PolicySeed>>(PoliciesRelativePath);
                return new IdentitySeedData(tenants, principals, policies);
            }

            private static T Deserialize<T>(string relativePath)
            {
                var json = ReadText(relativePath);
                return JsonSerializer.Deserialize<T>(json, SerializerOptions)
                    ?? throw new InvalidOperationException($"Failed to deserialize identity seed asset '{relativePath}'.");
            }
        }
    }

    public static string ResolveAssetPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new ArgumentException("Relative path must be provided", nameof(relativePath));
        }

        var normalized = relativePath.Replace('/', Path.DirectorySeparatorChar);
        var basePath = Path.Combine(AppContext.BaseDirectory, "TestAssets");
        return Path.GetFullPath(Path.Combine(basePath, normalized));
    }

    public static Stream OpenAsset(string relativePath)
    {
        var path = ResolveAssetPath(relativePath);
        return File.OpenRead(path);
    }

    public static string ReadText(string relativePath)
    {
        using var stream = OpenAsset(relativePath);
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: false);
        return reader.ReadToEnd();
    }

    public static string ComputeSha256(string relativePath)
    {
        using var stream = OpenAsset(relativePath);
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };
}
