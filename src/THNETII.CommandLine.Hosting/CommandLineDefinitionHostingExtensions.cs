using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides extesion methods for the .NET Generic Host library integration
    /// for System.CommandLine.
    /// </summary>
    public static class CommandLineDefinitionHostingExtensions
    {
        private static readonly Func<string[], IHostBuilder> defaultCreateHost =
            Host.CreateDefaultBuilder;

        /// <param name="definition">The command line definition instance for the application root command.</param>
        /// <seealso cref="HostingExtensions.UseHost(CommandLineBuilder, Func{string[], IHostBuilder}, Action{IHostBuilder})"/>
        public static CommandLineBuilder UseHostWithDefinition(
            this CommandLineBuilder cmdBuilder,
            CommandLineHostDefinition definition,
            Func<string[], IHostBuilder> createHostBuilder
            )
        {
            _ = definition ?? throw new ArgumentNullException(nameof(definition));
            return cmdBuilder.UseHost(createHostBuilder ?? defaultCreateHost,
                new Action<IHostBuilder>(host => ConfigureHostBuilderDefault(host, definition)) +
                definition.ConfigureHostBuilder);
        }

        private static void ConfigureHostBuilderDefault(IHostBuilder hostBuilder,
            CommandLineHostDefinition definition)
        {
            hostBuilder.ConfigureAppConfiguration((context, config) =>
            {
                var executorAssembly = definition.GetExecutorType()?.Assembly ??
                    Assembly.GetEntryAssembly();
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
            hostBuilder.ConfigureServices((context, services) =>
            {
                services.AddOptions<InvocationLifetimeOptions>()
                    .Configure<IConfiguration>((opts, config) =>
                        config.Bind("Lifetime", opts));
            });
        }
    }
}
