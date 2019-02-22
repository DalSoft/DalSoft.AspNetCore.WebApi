using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DalSoft.AspNetCore.WebApi.Serialization
{
    public static class Serialization
    {
        public static JsonSerializerSettings DefaultJsonSerializerSettings =>
            new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver { NamingStrategy = new SnakeCaseNamingStrategy { ProcessDictionaryKeys = true } },

                // All dates converted to UTC
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                
                NullValueHandling = NullValueHandling.Ignore,

                DefaultValueHandling = DefaultValueHandling.Include,

                MissingMemberHandling = MissingMemberHandling.Ignore,

                // Limit the object graph we'll consume to a fixed depth. This prevents stackoverflow exceptions from deserialization errors that might occur from deeply nested objects.
                MaxDepth = 32,

                // Do not change this setting. Setting this to None prevents Json.NET from loading malicious, unsafe, or security-sensitive types
                TypeNameHandling = TypeNameHandling.None,                
            };
    
        public static void ConfigureDefaultJsonSerializerSettings(this IServiceCollection services)
        {
            services.Configure<MvcJsonOptions>(options =>
            {
                options.SerializerSettings.ContractResolver = DefaultJsonSerializerSettings.ContractResolver;
                options.SerializerSettings.DateTimeZoneHandling = DefaultJsonSerializerSettings.DateTimeZoneHandling;
                options.SerializerSettings.NullValueHandling = DefaultJsonSerializerSettings.NullValueHandling;
                options.SerializerSettings.DefaultValueHandling = DefaultJsonSerializerSettings.DefaultValueHandling;
                options.SerializerSettings.MissingMemberHandling = DefaultJsonSerializerSettings.MissingMemberHandling;
                options.SerializerSettings.MaxDepth = DefaultJsonSerializerSettings.MaxDepth;
                options.SerializerSettings.TypeNameHandling = TypeNameHandling.None;
            });
        }

        internal static bool TrySerialize(this object @object, JsonSerializerSettings jsonSerializerSettings, out string result)
        {
            result = Serialize(@object, jsonSerializerSettings);

            return result != null;
        }

        internal static string Serialize(this object @object, JsonSerializerSettings jsonSerializerSettings)
        {
            return JsonConvert.SerializeObject(@object, jsonSerializerSettings);
        }

    }
}
