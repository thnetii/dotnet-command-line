using System.Collections.Generic;
using System.CommandLine;

using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Interface for specifying the command-line argument definition of a
    /// command.
    /// </summary>
    public interface ICommandLineHostingDefinition
    {
        /// <summary>
        /// Gets the Command symbol used by the command-line argument parser
        /// to parse the command-line arguments for the command that is being
        /// defined.
        /// </summary>
        Command Command { get; }

        /// <summary>
        /// Configures the specified .NET Generic Host builder with the
        /// configuration and service registrations that are specific to the
        /// command being defined.
        /// </summary>
        /// <param name="hostBuilder">The .NET Generic host builder instance to configure.</param>
        void ConfigureHostBuilder(IHostBuilder hostBuilder);

        /// <summary>
        /// Gets all added sub-commands for the current definition.
        /// </summary>
        IEnumerable<ICommandLineHostingDefinition> SubCommandDefinitions { get; }
    }
}
