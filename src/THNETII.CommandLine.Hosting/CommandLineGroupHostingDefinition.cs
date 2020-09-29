using System;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// An abstract base class for command groupings that don't have their own
    /// command handler and only define sub-commands.
    /// </summary>
    public abstract class CommandLineGroupHostingDefinition
        : CommandLineHostingDefinition<CommandLineGroupHostingDefinition>,
        ICommandLineExecutor
    {
        Task<int> ICommandLineExecutor.RunAsync(CancellationToken cancelToken)
        {
            throw new InvalidOperationException();
        }

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected override ICommandHandler CommandHandler =>
            throw new InvalidOperationException();

        /// <inheritdoc />
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected static new string GetAssemblyDescription() =>
            CommandLineHostingDefinition<CommandLineGroupHostingDefinition>
            .GetAssemblyDescription();
    }
}
