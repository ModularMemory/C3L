// ReSharper disable RedundantTypeArgumentsOfMethod

namespace GradebookAPI;

public static class Startup
{
    extension(IServiceCollection serviceCollection)
    {
        public IServiceCollection ConfigureServices()
        {
            serviceCollection
                .AddSingleton<TimeProvider>(TimeProvider.System);

            return serviceCollection;
        }
    }
}