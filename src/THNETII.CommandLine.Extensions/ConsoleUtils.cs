using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using THNETII.Common;

namespace THNETII.CommandLine.Extensions
{
    /// <summary>
    /// Provides utility methods for use with the default <see cref="Console"/> class.
    /// </summary>
    /// <seealso cref="Console"/>
    public static class ConsoleUtils
    {
        /// <summary>
        /// Executes the specified main-function asynchronously, while listening
        /// for the cancel key press event to cancel execution.
        /// </summary>
        /// <param name="asyncMain">The function to execute. Must not be <see langword="null"/>.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous execution of <paramref name="asyncMain"/> returning the process exit code upon completion.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="asyncMain"/> is <see langword="null"/>.</exception>
        /// <exception cref="OperationCanceledException">The <see cref="Console.CancelKeyPress"/> event was triggered, causing execution to be cancelled.</exception>
        /// <example>
        /// <code lang="CSharp">
        /// using System;
        /// using System.Threading;
        /// using System.Threading.Tasks;
        /// using THNETII.Common;
        /// using THNETII.Common.Cli;
        ///
        /// static class Program
        /// {
        ///     public static Task&lt;int&gt; Main() => ConsoleUtils.RunAsync(MainAsync);
        ///
        ///     public static async Task&lt;int&gt; MainAsync(CancellationToken cancelToken = default)
        ///     {
        ///         await Task.FromResult(ProcessExitCode.ExitSuccess);
        ///     }
        /// }
        /// </code>
        /// </example>
        public static async Task<int> RunAsync(Func<CancellationToken, Task> asyncMain)
        {
            if (asyncMain is null)
                throw new ArgumentNullException(nameof(asyncMain));
            using var cts = new CancellationTokenSource();
            var cancelKeyPressHandler = new ConsoleCancelEventHandler((sender, e) =>
            {
                // If cancellation already has been requested,
                // do not cancel process termination signal.
                e.Cancel = !cts.IsCancellationRequested;

                cts.Cancel(throwOnFirstException: true);
            });
            Console.CancelKeyPress += cancelKeyPressHandler;

            var cancelToken = cts.Token;
            try
            {
                var task = asyncMain(cancelToken);
                switch (task)
                {
                    case Task<int> intTask:
                        return await intTask.ConfigureAwait(continueOnCapturedContext: false);
                    case Task voidTask:
                        await voidTask.ConfigureAwait(continueOnCapturedContext: false);
                        break;
                }
                return ProcessExitCode.ExitSuccess;
            }
            finally
            {
                Console.CancelKeyPress -= cancelKeyPressHandler;
            }
        }

        private static string ReadLineOrThrowCancelled(CancellationToken cancelToken = default)
        {
            var line = Console.ReadLine();
            if (line is null)
                throw new OperationCanceledException(cancelToken);
            return line;
        }

        /// <summary>
        /// Performs an asynchronous, cancellable <see cref="Console.ReadLine"/> operation.
        /// </summary>
        /// <param name="cancelToken">An optional cancellation token that should be used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns the next line of characters from the input stream, or <see langword="null"/> if no more lines are
        /// available when completed.
        /// </returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="OperationCanceledException">The <see cref="Console.CancelKeyPress"/> event was triggered, causing execution to be cancelled.</exception>
        public static Task<string> ReadLineAsync(CancellationToken cancelToken = default) =>
            Task.Run(() => ReadLineOrThrowCancelled(cancelToken), cancelToken);

        private static bool ReadKeyAndAppend(bool printAsterisk, StringBuilder builder, out bool isCancelled)
        {
            isCancelled = false;

            bool continueReading = true;
            var key = Console.ReadKey(intercept: true);
            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    if (printAsterisk)
                    {
                        Console.Write(new string('\b', builder.Length));
                        Console.Write(new string(' ', builder.Length));
                        Console.Write(new string('\b', builder.Length));
                    }
                    builder.Clear();
                    break;
                case ConsoleKey.Backspace:
                    if (builder.Length < 1)
                        break;
                    if (printAsterisk)
                        Console.Write("\b \b");
                    builder.Remove(builder.Length - 1, 1);
                    break;
                case ConsoleKey.Enter:
                    continueReading = false;
                    break;
                case ConsoleKey.C when key.Modifiers is ConsoleModifiers.Control:
                case ConsoleKey.Pause when key.Modifiers is ConsoleModifiers.Control:
                    isCancelled = true;
                    break;
                case var _ when key.KeyChar != '\0':
                    char nextChar = key.KeyChar;
                    builder.Append(nextChar);
                    if (printAsterisk)
                        Console.Write('*');
                    break;
            }
            return continueReading;
        }

        /// <summary>
        /// Reads the next line of characters from the standard input stream,
        /// intercepting each typed character and optionally prints an asterisk (<c>'*'</c>) character instead.
        /// </summary>
        /// <param name="printAsterisk">If <see langword="true"/> (default) writes an asterisk for each typed character; otherwise, nothing is written to the console during input.</param>
        /// <returns>
        /// The next line of characters from the input stream, or <see langword="null"/> if no more lines are
        /// available.
        /// </returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <seealso cref="Console.ReadLine"/>
        /// <seealso cref="Console.ReadKey(bool)"/>
        public static string? ReadLineMasked(bool printAsterisk = true)
        {
            var builder = new StringBuilder();
            bool continueReading = true;
            while (continueReading)
            {
                continueReading = ReadKeyAndAppend(printAsterisk, builder, out bool isCancelled);

                if (isCancelled)
                    return null;
            }
            Console.WriteLine();
            return builder.ToString();
        }

        /// <summary>
        /// Asynchronously reads the next line of characters from the standard input stream,
        /// intercepting each typed character and writes an asterisk (<c>'*'</c>) character instead.
        /// </summary>
        /// <param name="cancelToken">An optional cancellation token that should be used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns the next line of characters from the input stream, or <see langword="null"/> if no more lines are
        /// available when completed.
        /// </returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="OperationCanceledException">The <see cref="Console.CancelKeyPress"/> event was triggered, causing execution to be cancelled.</exception>
        /// <seealso cref="Console.ReadLine"/>
        /// <seealso cref="Console.ReadKey(bool)"/>
        public static Task<string> ReadLineMaskedAsync(CancellationToken cancelToken) =>
            ReadLineMaskedAsync(printAsterisk: true, cancelToken);

        /// <summary>
        /// Asynchronously reads the next line of characters from the standard input stream,
        /// intercepting each typed character and optionally writes an asterisk (<c>'*'</c>) character instead.
        /// </summary>
        /// <param name="printAsterisk">If <see langword="true"/> (default) writes an asterisk for each typed character; otherwise, nothing is written to the console during input.</param>
        /// <param name="cancelToken">An optional cancellation token that should be used to cancel the operation.</param>
        /// <returns>
        /// A <see cref="Task{TResult}"/> that returns the next line of characters from the input stream, or <see langword="null"/> if no more lines are
        /// available when completed.
        /// available.
        /// </returns>
        /// <exception cref="System.IO.IOException">An I/O error occurred.</exception>
        /// <exception cref="OperationCanceledException">The <see cref="Console.CancelKeyPress"/> event was triggered, causing execution to be cancelled.</exception>
        /// <seealso cref="Console.ReadLine"/>
        /// <seealso cref="Console.ReadKey(bool)"/>
        public static async Task<string> ReadLineMaskedAsync(bool printAsterisk = true, CancellationToken cancelToken = default)
        {
            var builder = new StringBuilder();
            bool continueReading = true;
            while (continueReading)
            {
                while (!Console.KeyAvailable)
                {
                    cancelToken.ThrowIfCancellationRequested();
                    await Task.Yield();
                }
                continueReading = ReadKeyAndAppend(printAsterisk, builder, out bool isCancelled);

                if (isCancelled)
                    throw new OperationCanceledException(cancelToken);
            }
            Console.WriteLine();
            return builder.ToString();
        }
    }
}
