using System;
using System.CommandLine.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides extension methods related to command-line argument parsing on
    /// <see cref="IHostBuilder"/> instances.
    /// </summary>
    public static class CommandLineHostBuilderExtensions
    {
        /// <summary>
        /// Adds <see cref="InvocationLifetimeOptions"/> as an options type to
        /// the service registration of the .NET Generic Host DI-container.
        /// </summary>
        /// <param name="hostBuilder">The host builder instance to configure.</param>
        /// <returns><paramref name="hostBuilder"/></returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="hostBuilder"/> is <see langword="null"/>.
        /// </exception>
        public static IHostBuilder ConfigureCommandLineInvocation(
            this IHostBuilder hostBuilder)
        {
            _ = hostBuilder ?? throw new ArgumentNullException(nameof(hostBuilder));

            return hostBuilder.ConfigureServices((context, services) =>
            {
                const string configSection = "Lifetime";
                services.AddOptions<InvocationLifetimeOptions>()
                    .BindConfiguration(configSection);
            });
        }

        /// <seealso href="https://github.com/dotnet/runtime/pull/46740" />
        private static OptionsBuilder<TOptions> BindConfiguration<TOptions>(
            this OptionsBuilder<TOptions> optionsBuilder,
            string configSectionPath,
            Action<BinderOptions>? configureBinder = null)
            where TOptions : class
        {
            _ = optionsBuilder ?? throw new ArgumentNullException(nameof(optionsBuilder));
            _ = configSectionPath ?? throw new ArgumentNullException(nameof(configSectionPath));

            optionsBuilder.Configure<IConfiguration>((opts, config) =>
            {
                IConfiguration section = GetConfigurationSectionOrRoot(config, configSectionPath);
                section.Bind(opts, configureBinder);
            });
            optionsBuilder.Services.AddSingleton<IOptionsChangeTokenSource<TOptions>>(serviceProvider =>
            {
                var config = serviceProvider.GetRequiredService<IConfiguration>();
                IConfiguration section = GetConfigurationSectionOrRoot(config, configSectionPath);
                return new ConfigurationChangeTokenSource<TOptions>(optionsBuilder.Name, section);
            });

            return optionsBuilder;

            static IConfiguration GetConfigurationSectionOrRoot(IConfiguration config,
                string configSectionPath)
            {
                return string.IsNullOrEmpty(configSectionPath)
                    ? config
                    : config.GetSection(configSectionPath);
            }
        }
    }
}
