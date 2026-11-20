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
            serviceCollection.AddSingleton<TimeProvider>(TimeProvider.System);

            // Register the SQLite database context.
            serviceCollection.AddDbContext<GradebookDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("GradebookDatabase")));

            // Register the grade calculation service through its interface.
            serviceCollection.AddScoped<IGradeCalculationService, GradeCalculationService>();

            return serviceCollection;
        }
    }
}