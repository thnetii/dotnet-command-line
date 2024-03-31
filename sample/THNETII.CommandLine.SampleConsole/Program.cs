using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Hosting;
using System.CommandLine.Parsing;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using THNETII.CommandLine.Hosting;

namespace THNETII.CommandLine.SampleConsole
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            var cmdRoot = new RootCommand(CommandLineHost.GetAssemblyDescription(typeof(Program)))
            { Handler = CommandLineHost.GetCommandHandler<CommandLineApplication>() };
            cmdRoot.AddOption(new Option<string>(["--subject", "-s"])
            {
                Name = nameof(CommandLineOptions.Subject),
                Description = "The subject to greet",
                Argument = { Name = "NAME", Arity = ArgumentArity.ZeroOrOne },
            });

            var cmdParser = new CommandLineBuilder(cmdRoot)
                .UseDefaults()
                .UseHost(CreateHostBuilder)
                .Build();
            return cmdParser.InvokeAsync(args ?? []);
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = CommandLineHost
                .CreateDefaultBuilder(args ?? []);
            hostBuilder.ConfigureServices(services =>
            {
                services.AddOptions<CommandLineOptions>()
                    .Configure<IConfiguration>((opts, config) =>
                        config.Bind("CommandLineOptions", opts))
                    .BindCommandLine()
                    ;
            });

            return hostBuilder;
        }
    }

    public class CommandLineApplication(
        IOptions<CommandLineOptions> options,
        ILogger<CommandLineApplication>? logger = null) : ICommandLineExecutor
    {
        private readonly IOptions<CommandLineOptions> options =
            options ?? throw new ArgumentNullException(nameof(options));
        private readonly ILogger<CommandLineApplication> logger =
            logger ?? Microsoft.Extensions.Logging.Abstractions
                .NullLogger<CommandLineApplication>.Instance;

        public Task<int> RunAsync(CancellationToken cancelToken = default)
        {
            logger.LogInformation("Hello {Subject} from {Method}",
                options.Value.Subject, nameof(RunAsync));

            return Task.FromResult(0);
        }
    }

    public class CommandLineOptions
    {
        public string Subject { get; set; } = "World";
    }
}
