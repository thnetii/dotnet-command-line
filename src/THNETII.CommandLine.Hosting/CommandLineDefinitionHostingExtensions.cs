using System;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;

using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides extesion methods for the .NET Generic Host library integration
    /// for System.CommandLine.
    /// </summary>
    public static class CommandLineDefinitionHostingExtensions
    {
        /// <param name="cmdBuilder">The command line builder instance to configure.</param>
        /// <param name="definition">The command line definition instance for the application root command.</param>
        /// <param name="createHostBuilder">The construction function that creates a new <see cref="IHostBuilder"/> for the .NET Generic Host.</param>
        /// <seealso cref="HostingExtensions.UseHost(CommandLineBuilder, Func{string[], IHostBuilder}, Action{IHostBuilder})"/>
        public static CommandLineBuilder UseHostingDefinition<TExecutor>(
            this CommandLineBuilder cmdBuilder,
            CommandLineHostingDefinition<TExecutor> definition,
            Func<string[], IHostBuilder>? createHostBuilder = default
            )
            where TExecutor : ICommandLineExecutor
        {
            _ = definition ?? throw new ArgumentNullException(nameof(definition));
            return cmdBuilder.UseHost(createHostBuilder ?? CommandLineHost.DefaultBuilderFactory,
                definition.ConfigureHostBuilder);
        }
    }
}
