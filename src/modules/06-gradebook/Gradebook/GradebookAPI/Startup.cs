// ReSharper disable RedundantTypeArgumentsOfMethod
using GradebookAPI.Data;
using GradebookAPI.Services;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI;

public static class Startup
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection ConfigureServices(
            IConfiguration configuration)
        {
            serviceCollection
                .AddSingleton<TimeProvider>(TimeProvider.System);

            // Register the Gradebook database.
            serviceCollection.AddDbContext<GradebookDbContext>(options =>
                options.UseSqlite(
                    configuration.GetConnectionString("GradebookDatabase")));

            // Register the grade calculation service.
            serviceCollection.AddScoped<GradeCalculationService>();

            return serviceCollection;
        }
    }
}