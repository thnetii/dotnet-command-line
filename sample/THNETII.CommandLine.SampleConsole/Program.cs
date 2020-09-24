using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using THNETII.CommandLine.Hosting;

namespace THNETII.CommandLine.SampleConsole
{
    public static class Program
    {
        public static Task<int> Main(string[] args)
        {
            var definition = new CommandLineDefinition(typeof(Program));
            var cmdParser = new CommandLineBuilder(definition.Command)
                .UseDefaults()
                .UseHostWithDefinition(definition, CreateHostBuilder)
                .Build();
            return cmdParser.InvokeAsync(args ?? Array.Empty<string>());
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            var hostBuilder = Host.CreateDefaultBuilder(args ?? Array.Empty<string>());

            return hostBuilder;
        }

        public static void Run(IHost host)
        {
            _ = host ?? throw new ArgumentNullException(nameof(host));

            using var serviceScope = host.Services.CreateScope();
            var serviceProvider = serviceScope.ServiceProvider;

            var loggerFactory = serviceProvider.GetService<ILoggerFactory>()
                ?? Microsoft.Extensions.Logging.Abstractions.NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger(typeof(Program));

            logger.LogInformation("Hello from {Method}", nameof(Run));
        }
    }

    public class CommandLineDefinition : CommandLineHostDefinition
    {
        public CommandLineDefinition(Type executorType) : base(executorType)
        {
            Command = new RootCommand(GetAssemblyDescription(executorType))
            { Handler = GetCommandHandler(executorType) };
        }

        public override Command Command { get; }
    }
}
