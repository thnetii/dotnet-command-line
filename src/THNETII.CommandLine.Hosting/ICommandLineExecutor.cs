using System.Threading;
using System.Threading.Tasks;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Interface for executing a command-line command within a .NET Generic
    /// Host application.
    /// </summary>
    public interface ICommandLineExecutor
    {
        /// <summary>
        /// Runs the command.
        /// </summary>
        /// <param name="cancelToken">An optional cancellation token that signals that the user requested termination.</param>
        /// <returns>
        /// A Task instance representing the asynchronous execution of the
        /// command. When the taks has run to completion, the integer result is
        /// the process exit code value representing the result of running the
        /// command. <c>0</c> (zero) indicates success.
        /// </returns>
        Task<int> RunAsync(CancellationToken cancelToken = default);
    }
}
