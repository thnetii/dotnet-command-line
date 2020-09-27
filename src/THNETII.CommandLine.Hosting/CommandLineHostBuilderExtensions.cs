using System;
using System.CommandLine.Hosting;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
                services.AddOptions<InvocationLifetimeOptions>()
                    .Configure<IConfiguration>((opts, config) =>
                        config.Bind("Lifetime", opts));
            });
        }
    }
}
