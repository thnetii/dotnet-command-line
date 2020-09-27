using System;
using System.CommandLine.Hosting;
using System.Reflection;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides convenience methods for creating instances of
    /// <see cref="IHostBuilder"/> using pre-configures defaults, including
    /// additional defaults that are suited for command-line applications.
    /// </summary>
    /// <seealso cref="Host"/>
    public static class CommandLineHost
    {
        /// <summary>
        /// Creates a new instance of the <see cref="HostBuilder"/> class using
        /// the defaults configured by <see cref="Host.CreateDefaultBuilder(string[])"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <remarks>
        /// Additionally added pre-configured defaults for command-line applications:
        /// <list type="bullet">
        /// <item>load app <see cref="IConfiguration"/> from embedded <c>appsettings.json</c> and <c>appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json</c> <strong>before</strong> the configuration is loaded from the physical file system.</item>
        /// <item>Add the <see cref="InvocationLifetimeOptions"/> type as an option service to the DI-service registration and configure it to be bound from the <see cref="IConfiguration"/> section named <c>Lifetime</c>.</item>
        /// </list>
        /// </remarks>
        public static IHostBuilder CreateDefaultBuilder(string[]? args)
        {
            return Host.CreateDefaultBuilder(args ?? Array.Empty<string>())
                .ConfigureEmbeddedAppConfiguration(Assembly.GetEntryAssembly()!)
                .ConfigureCommandLineInvocation();
        }

        internal static Func<string[], IHostBuilder> DefaultBuilderFactory { get; } =
            CreateDefaultBuilder;
    }
}
