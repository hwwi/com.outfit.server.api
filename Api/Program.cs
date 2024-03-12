using System.IO;
using Autofac.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
            => Host.CreateDefaultBuilder(args)
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
                // .ConfigureLogging(logging =>
                // {
                //     logging.AddAWSProvider();
                // })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseContentRoot(Directory.GetCurrentDirectory())
                        .UseIISIntegration()
                        .UseStartup<Startup>();
                })
                .ConfigureAppConfiguration((context, builder) =>
                {
                    builder.AddSystemsManager("/AspNetCore/Api/Base");
                    var env = context.HostingEnvironment?.EnvironmentName;
                    if (env != null)
                        builder.AddSystemsManager($"/AspNetCore/Api/{env}");
                });
    }
}