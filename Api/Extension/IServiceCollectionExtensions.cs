using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Api.Extension
{
    public static class IServiceCollectionExtensions
    {
        public static TOptions ConfigurationSection<TOptions>(
            this IServiceCollection services,
            IConfiguration config)
            where TOptions : class
        {
            services.Configure<TOptions>(config);
            return config.Get<TOptions>();
        }
    }
}