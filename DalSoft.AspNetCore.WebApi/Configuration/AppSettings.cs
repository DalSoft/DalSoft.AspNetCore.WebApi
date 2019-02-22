using System.IO;
using Microsoft.Extensions.Configuration;

namespace DalSoft.AspNetCore.WebApi.Configuration
{
    public abstract class AppSettings<TSettings> where TSettings : class, new()
    {
        private static TSettings _appSettings;

        public static TSettings GetSettings()
        {
            if (_appSettings != null)
                return _appSettings;

            _appSettings = new TSettings();
            GetConfig().GetSection(typeof(TSettings).Name).Bind(_appSettings);

            return _appSettings;
        }

        protected static IConfigurationRoot GetConfig(string environmentName=null)
        {
            var binFolder = Directory.GetCurrentDirectory();

            return new ConfigurationBuilder()
                .SetBasePath(binFolder)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{environmentName}.json", optional: true)
                .Build();
        }
    }
}
