using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Hartonomous.IntegrationTests.Tests.Workflows;

/// <summary>
/// Integration tests for embedding generation workflow.
/// Tests asynchronous embedding generation and vector storage.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Category", "Embedding")]
public class EmbeddingGenerationWorkflowTests : IntegrationTestBase<WebApplicationFactory<Program>>
{
    public EmbeddingGenerationWorkflowTests(WebApplicationFactory<Program> factory) : base(factory) { }

    [Fact]
    public async Task Workflow_HealthCheck_ReturnsHealthy()
    {
        // Arrange
        var client = GetClient();

        // Act
        var response = await client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
    }

    #region Text Embedding Tests

    [Fact]
    public async Task Workflow_TextAtom_GeneratesEmbedding()
    {
        // Arrange
        var textContent = "This is a test document for embedding generation.";
        var atomId = await InsertTextAtom(textContent);

        // Act
        await TriggerEmbeddingGeneration(atomId);
        await WaitForEmbeddingCompletion(atomId, timeout: TimeSpan.FromSeconds(30));

        // Assert
        var embedding = await GetAtomEmbedding(atomId);
        embedding.Should().NotBeNull();
        embedding.Length.Should().Be(1536 * 4); // OpenAI embedding size (float32)
    }

    [Fact]
    public async Task Workflow_MultipleAtoms_GeneratesAllEmbeddings()
    {
        // Arrange
        var atom1 = await InsertTextAtom("First document");
        var atom2 = await InsertTextAtom("Second document");
        var atom3 = await InsertTextAtom("Third document");

        // Act
        await TriggerBatchEmbeddingGeneration(new[] { atom1, atom2, atom3 });
        await WaitForAllEmbeddingsCompletion(new[] { atom1, atom2, atom3 });

        // Assert
        var embedding1 = await GetAtomEmbedding(atom1);
        var embedding2 = await GetAtomEmbedding(atom2);
        var embedding3 = await GetAtomEmbedding(atom3);

        embedding1.Should().NotBeNull();
        embedding2.Should().NotBeNull();
        embedding3.Should().NotBeNull();
    }

    #endregion

    #region Image Embedding Tests

    [Fact]
    public async Task Workflow_ImageAtom_GeneratesVisionEmbedding()
    {
        // Arrange
        var imageData = CreateTestImage();
        var atomId = await InsertImageAtom(imageData);

        // Act
        await TriggerEmbeddingGeneration(atomId);
        await WaitForEmbeddingCompletion(atomId);

        // Assert
        var embedding = await GetAtomEmbedding(atomId);
        embedding.Should().NotBeNull();
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task Workflow_InvalidAtomId_HandlesGracefully()
    {
        // Arrange
        var invalidAtomId = -1;

        // Act
        Func<Task> act = async () => await TriggerEmbeddingGeneration(invalidAtomId);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task Workflow_EmptyText_SkipsEmbedding()
    {
        // Arrange
        var atomId = await InsertTextAtom("");

        // Act
        await TriggerEmbeddingGeneration(atomId);
        await Task.Delay(1000);

        // Assert
        var embedding = await GetAtomEmbedding(atomId);
        embedding.Should().BeNull(); // Empty text should not generate embedding
    }

    #endregion

    #region Helper Methods

    private async Task<int> InsertTextAtom(string text)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(text);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        
        return await ExecuteScalarAsync<int>(
            @"INSERT INTO Atoms (AtomicValue, ContentHash, Modality, TenantId) 
              VALUES (@AtomicValue, @ContentHash, @Modality, @TenantId);
              SELECT SCOPE_IDENTITY();",
            new System.Data.SqlClient.SqlParameter("@AtomicValue", bytes),
            new System.Data.SqlClient.SqlParameter("@ContentHash", hash),
            new System.Data.SqlClient.SqlParameter("@Modality", "text"),
            new System.Data.SqlClient.SqlParameter("@TenantId", 1));
    }

    private async Task<int> InsertImageAtom(byte[] imageData)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(imageData);
        
        return await ExecuteScalarAsync<int>(
            @"INSERT INTO Atoms (AtomicValue, ContentHash, Modality, TenantId) 
              VALUES (@AtomicValue, @ContentHash, @Modality, @TenantId);
              SELECT SCOPE_IDENTITY();",
            new System.Data.SqlClient.SqlParameter("@AtomicValue", imageData),
            new System.Data.SqlClient.SqlParameter("@ContentHash", hash),
            new System.Data.SqlClient.SqlParameter("@Modality", "image"),
            new System.Data.SqlClient.SqlParameter("@TenantId", 1));
    }

    private async Task TriggerEmbeddingGeneration(int atomId)
    {
        await ExecuteNonQueryAsync(
            "EXEC sp_EnqueueEmbeddingGeneration @AtomId",
            new System.Data.SqlClient.SqlParameter("@AtomId", atomId));
    }

    private async Task TriggerBatchEmbeddingGeneration(int[] atomIds)
    {
        foreach (var atomId in atomIds)
        {
            await TriggerEmbeddingGeneration(atomId);
        }
    }

    private async Task WaitForEmbeddingCompletion(int atomId, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(10));
        
        while (DateTime.UtcNow < deadline)
        {
            var embedding = await GetAtomEmbedding(atomId);
            if (embedding != null) return;
            
            await Task.Delay(100);
        }
    }

    private async Task WaitForAllEmbeddingsCompletion(int[] atomIds)
    {
        foreach (var atomId in atomIds)
        {
            await WaitForEmbeddingCompletion(atomId);
        }
    }

    private async Task<byte[]?> GetAtomEmbedding(int atomId)
    {
        return await ExecuteScalarAsync<byte[]>(
            "SELECT Embedding FROM Atoms WHERE AtomId = @AtomId",
            new System.Data.SqlClient.SqlParameter("@AtomId", atomId));
    }

    private byte[] CreateTestImage()
    {
        // Minimal PNG header
        return new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
    }

    #endregion
}
