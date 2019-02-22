using Microsoft.Extensions.Configuration;

namespace DalSoft.AspNetCore.WebApi.Configuration
{
    public class ConcurrentConfigurationSource<TConfig> : IConfigurationSource where TConfig : class, new()
    {
        private readonly ConcurrentConfiguration<TConfig> _concurrentConfiguration;

        public ConcurrentConfigurationSource(ConcurrentConfiguration<TConfig> concurrentConfiguration)
        {
            _concurrentConfiguration = concurrentConfiguration;
        }
        
        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            return new ConcurrentConfigurationProvider<TConfig>(_concurrentConfiguration);
        }
    }
}