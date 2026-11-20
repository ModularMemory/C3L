// ReSharper disable RedundantTypeArgumentsOfMethod
using GradebookAPI.Services;
using GradebookAPI.Services.Interfaces;
namespace GradebookAPI;

public static class Startup
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection ConfigureServices()
        {
            serviceCollection
                .AddSingleton<TimeProvider>(TimeProvider.System);
            serviceCollection
                .AddScoped<IGradeCalculationService, GradeCalculationService>();

            return serviceCollection;
        }
    }
}