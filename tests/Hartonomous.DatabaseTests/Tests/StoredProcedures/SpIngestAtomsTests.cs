using FluentAssertions;
using Hartonomous.DatabaseTests.Infrastructure;
using Microsoft.Data.SqlClient;
using System.Data;
using Xunit;
using Xunit.Abstractions;

namespace Hartonomous.DatabaseTests.Tests.StoredProcedures;

/// <summary>
/// Tests for sp_IngestAtoms stored procedure.
/// Validates bulk atom insertion, deduplication, and composition linking.
/// </summary>
[Trait("Category", "Database")]
[Trait("Category", "StoredProcedure")]
public class SpIngestAtomsTests : DatabaseTestBase
{
    public SpIngestAtomsTests(ITestOutputHelper output) : base() { }

    #region Basic Ingestion Tests

    [Fact]
    public async Task SpIngestAtoms_SingleAtom_InsertsSuccessfully()
    {
        // Arrange
        var atom = CreateTestAtom("test-hash", "test content");

        // Act
        var atomId = await ExecuteScalarAsync<int>(
            "EXEC sp_IngestAtoms @AtomicValue, @ContentHash, @Modality, @TenantId",
            new SqlParameter("@AtomicValue", SqlDbType.VarBinary) { Value = atom.AtomicValue },
            new SqlParameter("@ContentHash", SqlDbType.VarBinary) { Value = atom.ContentHash },
            new SqlParameter("@Modality", SqlDbType.NVarChar) { Value = atom.Modality },
            new SqlParameter("@TenantId", SqlDbType.Int) { Value = 1 });

        // Assert
        atomId.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task SpIngestAtoms_DuplicateHash_ReturnsExistingId()
    {
        // Arrange
        var atom = CreateTestAtom("duplicate-hash", "content");
        var firstId = await InsertAtom(atom);

        // Act - Insert same hash again
        var secondId = await ExecuteScalarAsync<int>(
            "EXEC sp_IngestAtoms @AtomicValue, @ContentHash, @Modality, @TenantId",
            new SqlParameter("@AtomicValue", SqlDbType.VarBinary) { Value = atom.AtomicValue },
            new SqlParameter("@ContentHash", SqlDbType.VarBinary) { Value = atom.ContentHash },
            new SqlParameter("@Modality", SqlDbType.NVarChar) { Value = atom.Modality },
            new SqlParameter("@TenantId", SqlDbType.Int) { Value = 1 });

        // Assert
        secondId.Should().Be(firstId);
    }

    #endregion

    #region Bulk Ingestion Tests

    [Fact]
    public async Task SpIngestAtoms_MultipleAtoms_InsertsAll()
    {
        // Arrange
        var atoms = new[]
        {
            CreateTestAtom("hash1", "content1"),
            CreateTestAtom("hash2", "content2"),
            CreateTestAtom("hash3", "content3")
        };

        // Act
        var ids = new List<int>();
        foreach (var atom in atoms)
        {
            var id = await InsertAtom(atom);
            ids.Add(id);
        }

        // Assert
        ids.Should().HaveCount(3);
        ids.Should().OnlyHaveUniqueItems();
    }

    #endregion

    #region Tenant Isolation Tests

    [Fact]
    public async Task SpIngestAtoms_DifferentTenants_IsolatesData()
    {
        // Arrange
        var atom1 = CreateTestAtom("tenant-hash", "content");
        
        // Act
        var tenant1Id = await InsertAtom(atom1, tenantId: 1);
        var tenant2Id = await InsertAtom(atom1, tenantId: 2);

        // Assert
        tenant1Id.Should().NotBe(tenant2Id); // Different tenants = different IDs
    }

    #endregion

    #region Helper Methods

    private TestAtom CreateTestAtom(string hash, string content)
    {
        return new TestAtom
        {
            AtomicValue = System.Text.Encoding.UTF8.GetBytes(content),
            ContentHash = System.Text.Encoding.UTF8.GetBytes(hash),
            Modality = "text"
        };
    }

    private async Task<int> InsertAtom(TestAtom atom, int tenantId = 1)
    {
        return await ExecuteScalarAsync<int>(
            "EXEC sp_IngestAtoms @AtomicValue, @ContentHash, @Modality, @TenantId",
            new SqlParameter("@AtomicValue", SqlDbType.VarBinary) { Value = atom.AtomicValue },
            new SqlParameter("@ContentHash", SqlDbType.VarBinary) { Value = atom.ContentHash },
            new SqlParameter("@Modality", SqlDbType.NVarChar) { Value = atom.Modality },
            new SqlParameter("@TenantId", SqlDbType.Int) { Value = tenantId });
    }

    private class TestAtom
    {
        public byte[] AtomicValue { get; set; }
        public byte[] ContentHash { get; set; }
        public string Modality { get; set; }
    }

    #endregion
}
