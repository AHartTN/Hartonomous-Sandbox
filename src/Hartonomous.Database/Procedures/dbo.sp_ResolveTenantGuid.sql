-- =============================================
-- Procedure: dbo.sp_ResolveTenantGuid
-- Description: Safely resolves Azure Entra tenant GUID to internal integer TenantId
-- Purpose: Provides ACID-compliant GUID->INT mapping with auto-registration
-- Replaces: Unsafe GetHashCode() approach
-- =============================================

CREATE PROCEDURE [dbo].[sp_ResolveTenantGuid]
    @TenantGuid UNIQUEIDENTIFIER,
    @TenantName NVARCHAR(200) = NULL,
    @AutoRegister BIT = 1,
    @TenantId INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    -- Try to find existing mapping
    SELECT @TenantId = TenantId
    FROM dbo.TenantGuidMapping WITH (NOLOCK)
    WHERE TenantGuid = @TenantGuid
      AND IsActive = 1;

    -- If found, return immediately
    IF @TenantId IS NOT NULL
        RETURN 0;

    -- If not found and auto-registration enabled, create new mapping
    IF @AutoRegister = 1
    BEGIN
        BEGIN TRANSACTION;
        BEGIN TRY
            -- Double-check within transaction to prevent race conditions
            SELECT @TenantId = TenantId
            FROM dbo.TenantGuidMapping WITH (UPDLOCK, HOLDLOCK)
            WHERE TenantGuid = @TenantGuid;

            IF @TenantId IS NULL
            BEGIN
                -- Insert new tenant mapping
                INSERT INTO dbo.TenantGuidMapping (
                    TenantGuid,
                    TenantName,
                    CreatedBy
                )
                VALUES (
                    @TenantGuid,
                    @TenantName,
                    SUSER_SNAME()
                );

                SET @TenantId = SCOPE_IDENTITY();

                -- Log tenant registration
                DECLARE @LogMessage NVARCHAR(500) =
                    N'Auto-registered new tenant: GUID=' + CAST(@TenantGuid AS NVARCHAR(36)) +
                    N', TenantId=' + CAST(@TenantId AS NVARCHAR(10)) +
                    N', Name=' + ISNULL(@TenantName, N'(unnamed)');

                RAISERROR(@LogMessage, 10, 1) WITH LOG;
            END

            COMMIT TRANSACTION;
            RETURN 0;
        END TRY
        BEGIN CATCH
            IF @@TRANCOUNT > 0
                ROLLBACK TRANSACTION;

            THROW;
        END CATCH
    END
    ELSE
    BEGIN
        -- Auto-registration disabled and tenant not found
        DECLARE @ErrorMessage NVARCHAR(500) =
            N'Tenant GUID not found and auto-registration disabled: ' + CAST(@TenantGuid AS NVARCHAR(36));

        RAISERROR(@ErrorMessage, 16, 1);
        RETURN 1;
    END
END
GO

EXEC sys.sp_addextendedproperty
    @name = N'MS_Description',
    @value = N'Safely resolves Azure Entra tenant GUID to internal integer TenantId. Supports auto-registration for new tenants with ACID guarantees to prevent race conditions. Returns TenantId via OUTPUT parameter.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'PROCEDURE', @level1name = N'sp_ResolveTenantGuid';
GO
