using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hartonomous.Data
{
    /// <summary>
    /// Design-time factory for EF Core migrations
    /// </summary>
    public class HartonomousDbContextFactory : IDesignTimeDbContextFactory<HartonomousDbContext>
    {
        public HartonomousDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<HartonomousDbContext>();
            
            // Design-time connection string
            optionsBuilder.UseSqlServer(
                "Server=localhost;Database=Hartonomous;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true",
                sqlServerOptions =>
                {
                    sqlServerOptions.EnableRetryOnFailure(
                        maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null);
                    
                    // CRITICAL: Enable NetTopologySuite for GEOMETRY columns
                    sqlServerOptions.UseNetTopologySuite();
                });

            return new HartonomousDbContext(optionsBuilder.Options);
        }
    }
}
