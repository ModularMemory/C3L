// ReSharper disable RedundantTypeArgumentsOfMethod

using GradebookAPI.Data;
using GradebookAPI.Services;
using GradebookAPI.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GradebookAPI;

public static class Startup
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection ConfigureServices(IConfiguration configuration)
        {
            serviceCollection
                .AddSingleton<TimeProvider>(TimeProvider.System)
                .AddScoped<IGradeCalculationService, GradeCalculationService>();

            // Register the SQLite database context.
            serviceCollection.AddDbContext<GradebookDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("GradebookDatabase")));

            return serviceCollection;
        }
    }
}