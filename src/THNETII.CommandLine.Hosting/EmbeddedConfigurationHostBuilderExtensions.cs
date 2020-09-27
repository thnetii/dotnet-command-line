using System;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides extensions methods that configure a .NET Generic Host
    /// <see cref="IHostBuilder"/> instance to use embedded application
    /// configuration.
    /// </summary>
    public static class EmbeddedConfigurationHostBuilderExtensions
    {
        /// <summary>
        /// Inserts application configuration embedded in the specified assembly
        /// at the top of the list of application-specific configuration
        /// providers.
        /// </summary>
        /// <param name="hostBuilder">
        /// The builder instance for a.NET Generic Host application.
        /// </param>
        /// <param name="assembly">
        /// The assembly containing the embedded application configuration files.
        /// </param>
        /// <returns><paramref name="hostBuilder"/></returns>
        public static IHostBuilder ConfigureEmbeddedAppConfiguration(
            this IHostBuilder hostBuilder, Assembly assembly)
        {
            _ = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

            if (assembly is null)
                return hostBuilder;

            return hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                var executorAssembly = assembly;
                var fileProvider = new EmbeddedFileProvider(executorAssembly);
                var hostingEnvironment = context.HostingEnvironment;

                var sources = config.Sources;
                int originalSourcesCount = sources.Count;

                config.AddJsonFile(fileProvider,
                    $"appsettings.json",
                    optional: true, reloadOnChange: true);
                config.AddJsonFile(fileProvider,
                    $"appsettings.{hostingEnvironment.EnvironmentName}.json",
                    optional: true, reloadOnChange: true);

                const int insert_idx = 1;
                for (int i_dst = insert_idx, i_src = originalSourcesCount;
                    i_src < sources.Count; i_dst++, i_src++)
                {
                    var configSource = sources[i_src];
                    sources.RemoveAt(i_src);
                    sources.Insert(i_dst, configSource);
                }
            });
        }
    }
}
