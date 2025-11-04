using Hartonomous.Core.Services;

namespace Hartonomous.UnitTests.Core;

// TODO: These tests are OBSOLETE after architectural refactoring.
// ModelCapabilityService now queries IModelRepository instead of hardcoding model names.
// Tests need to be rewritten to:
// 1. Mock IModelRepository.GetByNameAsync to return Model entities with Metadata
// 2. Test JSON parsing of SupportedTasks/SupportedModalities
// 3. Test fallback to DefaultCapabilities when model not found
// 
// See commit message for architectural rationale:
// - Removed hardcoded third-party model names (gpt-4, dall-e, whisper)
// - Replaced with database queries to Model.Metadata.SupportedTasks/SupportedModalities
// - Honors database-native architecture: "everything atomizes, everything becomes queryable"
//
// BLOCKED: Requires Moq or NSubstitute for async repository mocking.

#pragma warning disable xUnit1013 // Public method should be marked as test
public sealed class ModelCapabilityServiceTests
{
    // All tests disabled - obsolete after architectural refactoring
    // See TODO comment above for details
}
#pragma warning restore xUnit1013
