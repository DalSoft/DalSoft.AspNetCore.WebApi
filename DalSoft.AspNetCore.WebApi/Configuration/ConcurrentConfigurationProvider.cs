using Microsoft.Extensions.Configuration;

namespace DalSoft.AspNetCore.WebApi.Configuration
{
    public class ConcurrentConfigurationProvider<T> : ConfigurationProvider where T : class, new()
    {
        public ConcurrentConfigurationProvider(ConcurrentConfiguration<T> concurrentConfiguration)
        {
            Data = concurrentConfiguration.ConcurrentDictionary;
        }
    }
}