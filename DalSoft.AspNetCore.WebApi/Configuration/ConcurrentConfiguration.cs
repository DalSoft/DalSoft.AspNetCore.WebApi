using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace DalSoft.AspNetCore.WebApi.Configuration
{
    public class ConcurrentConfiguration<T>  where T : class, new()
    {
        internal ConcurrentDictionary<string, string> ConcurrentDictionary { get; }

        public ConcurrentConfiguration()
        {
            ConcurrentDictionary = new ConcurrentDictionary<string, string>();
        }

        public void AddOrUpdateConfiguration<TValue>(Expression<Func<T, TValue>> member, string value)
        {
            if (!(member.Body is MemberExpression)) 
                return;

            var configSelector = $"{typeof(T).Name}:{string.Join(":", member.Body.ToString().Split(".".ToCharArray()).Skip(1))}";
            
            AddOrUpdateConfiguration(configSelector, value);
        }

        public void AddOrUpdateConfiguration(string member, string value)
        {
            ConcurrentDictionary.AddOrUpdate(member, value, (key, oldValue) => value);
        }
    }
}