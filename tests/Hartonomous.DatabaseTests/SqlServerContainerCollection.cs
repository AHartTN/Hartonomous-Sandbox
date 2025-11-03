using Hartonomous.DatabaseTests.Fixtures;
using Xunit;

namespace Hartonomous.DatabaseTests;

[CollectionDefinition("SqlServerContainer")]
public sealed class SqlServerContainerCollection : ICollectionFixture<SqlServerContainerFixture>
{
}
