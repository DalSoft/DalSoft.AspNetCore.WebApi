using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DalSoft.AspNetCore.WebApi.Configuration
{
    public static class Extensions
    {
        public static IConfigurationBuilder AddConcurrentConfiguration<TConfig>(this IConfigurationBuilder configurationBuilder, ConcurrentConfiguration<TConfig> concurrentConfiguration) where TConfig : class, new()
        {
            configurationBuilder.Add(new ConcurrentConfigurationSource<TConfig>(concurrentConfiguration));
            return configurationBuilder;
        }

        public static IServiceCollection AddConcurrentConfiguration<TConfig>(this IServiceCollection services, ConcurrentConfiguration<TConfig> concurrentConfiguration) where TConfig : class, new()
        {
            return services.AddSingleton(concurrentConfiguration);;
        }
    }
}
