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
            var definition = new CommandLineDefinition();
            var cmdParser = new CommandLineBuilder(definition.Command)
                .UseDefaults()
                .UseHostingDefinition(definition, CreateHostBuilder)
                .Build();
            return cmdParser.InvokeAsync(args ?? Array.Empty<string>());
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args ?? Array.Empty<string>())
                .ConfigureEmbeddedAppConfiguration(typeof(Program).Assembly)
                .ConfigureCommandLineInvocation()
                ;

            hostBuilder.ConfigureServices(serivces =>
            {
                serivces.AddOptions<CommandLineOptions>()
                    .Configure<IConfiguration>((opts, config) =>
                        config.Bind("CommandLine", opts)
                        )
                    .BindCommandLine()
                    ;
            });

            return hostBuilder;
        }
    }

    public class CommandLineApplication : ICommandLineExecutor
    {
        private readonly CommandLineOptions options;
        private readonly ILogger<CommandLineApplication> logger;

        public CommandLineApplication(
            IOptions<CommandLineOptions> options,
            ILogger<CommandLineApplication>? logger = null)
        {
            this.options = options?.Value
                ?? throw new ArgumentNullException(nameof(options));
            this.logger = logger ?? Microsoft.Extensions.Logging.Abstractions
                .NullLogger<CommandLineApplication>.Instance;
        }

        public Task<int> RunAsync(CancellationToken cancelToken = default)
        {
            logger.LogInformation("Hello {Subject} from {Method}",
                options.Subject, nameof(RunAsync));

            return Task.FromResult(0);
        }
    }

    public class CommandLineOptions
    {
        public string Subject { get; set; } = "World";
    }

    public class CommandLineDefinition : CommandLineHostingDefinition<CommandLineApplication>
    {
        public CommandLineDefinition() : base()
        {
            Command = new RootCommand(GetAssemblyDescription())
            { Handler = CommandHandler };

            SubjectArgument = new Argument<string>()
            {
                Name = nameof(CommandLineOptions.Subject),
                Arity = ArgumentArity.ZeroOrOne,
            };
            Command.AddArgument(SubjectArgument);
        }

        public override Command Command { get; }
        public Argument<string> SubjectArgument { get; }
    }
}
