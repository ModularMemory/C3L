namespace GradebookAPI.Extensions;

/// <summary>Provides extensions for <see cref="IServiceCollection"/>s.</summary>
public static class ServiceCollectionExtensions
{
    extension(IServiceCollection serviceCollection)
    {
        /// <summary>Add an <see cref="IHostedService"/> registration for the given type.</summary>
        /// <typeparam name="TService">The interface of the hosted service.</typeparam>
        /// <typeparam name="TImplementation">The implementation of the hosted service.</typeparam>
        public IServiceCollection AddHostedService<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService, IHostedService
        {
            return serviceCollection
                .AddSingleton<TService, TImplementation>()
                .AddHostedService<TImplementation>(provider => (TImplementation)provider.GetRequiredService<TService>());
        }
    }
}