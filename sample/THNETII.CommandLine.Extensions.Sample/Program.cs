using System;
using System.Threading;
using System.Threading.Tasks;

namespace THNETII.CommandLine.Extensions.Sample
{
    public static class Program
    {
        public static Task<int> Main() => ConsoleUtils.RunAsync(MainAsync);

        public static async Task MainAsync(CancellationToken cancelToken = default)
        {
            var line = await ConsoleUtils.ReadLineAsync(cancelToken);
            cancelToken.ThrowIfCancellationRequested();
            Console.WriteLine("Read line: " + line ?? string.Empty);

            Console.WriteLine();
            Console.Write("Password: ");
            var password = await ConsoleUtils.ReadLineMaskedAsync(cancelToken);
            Console.WriteLine($"Entered password has {password.Length} characters");

            await Task.CompletedTask;
        }
    }
}
