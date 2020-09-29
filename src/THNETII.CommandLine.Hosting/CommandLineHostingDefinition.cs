using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using CommandHandlerFactory = System.CommandLine.Invocation.CommandHandler;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// An abstract base class where all Symbols of a command can be stored
    /// together in oder to simplify Command-line argument binding for
    /// .NET Generic Host scenarios.
    /// </summary>
    public abstract class CommandLineHostingDefinition<TExecutor>
        : ICommandLineHostingDefinition
        where TExecutor : ICommandLineExecutor
    {
        private readonly List<ICommandLineHostingDefinition> subCommandDefinitionList =
            new List<ICommandLineHostingDefinition>();

        /// <summary>
        /// Gets the <see cref="Command"/> that is definined by this instance.
        /// </summary>
        public abstract Command Command { get; }

        /// <summary>
        /// Adds a command definition as a sub-command to the command defined by
        /// the current instance. The <see cref="Command"/> property of
        /// <paramref name="definition"/> is added using
        /// <see cref="Command.AddCommand(Command)"/>.
        /// </summary>
        /// <param name="definition">
        /// A command definition instance defining the sub-command to add.
        /// </param>
        public void AddSubCommandDefinition<TSubExecutor>(
            CommandLineHostingDefinition<TSubExecutor> definition)
            where TSubExecutor : ICommandLineExecutor
        {
            _ = definition ?? throw new ArgumentNullException(nameof(definition));

            if (definition.Command is Command subCommand)
                Command.AddCommand(subCommand);
            subCommandDefinitionList.Add(definition);
        }

        /// <inheritdoc />
        public IEnumerable<ICommandLineHostingDefinition> SubCommandDefinitions =>
            subCommandDefinitionList;

        /// <summary>
        /// The configuration method that configures a .NET Generic Host builder
        /// to use this command definition.
        /// </summary>
        /// <param name="hostBuilder">The Host builder to configure.</param>
        public virtual void ConfigureHostBuilder(IHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                services.AddSingleton(GetType(), this));

            foreach (var subDef in SubCommandDefinitions)
                subDef.ConfigureHostBuilder(hostBuilder);
        }

        /// <summary>
        /// Gets a value for the <see cref="ISymbol.Description"/> property of
        /// the definied <see cref="Command"/>. The assembly containing the
        /// specified executor type is probed for assembly attributes.
        /// </summary>
        /// <returns>
        /// Attempts to find an appropriate description string using the
        /// following sources in order. The first non-empty string is returned:
        /// <list type="number">
        /// <item>
        /// The <see cref="AssemblyDescriptionAttribute.Description"/> property
        /// of the <see cref="AssemblyDescriptionAttribute"/> declared on the
        /// assembly containing <typeparamref name="TExecutor"/>.
        /// </item>
        /// <item>
        /// The <see cref="AssemblyProductAttribute.Product"/> property
        /// of the <see cref="AssemblyProductAttribute"/> declared on the
        /// assembly containing <typeparamref name="TExecutor"/>.
        /// </item>
        /// <item>
        /// The <see langword="namespace"/> that
        /// <typeparamref name="TExecutor"/> is defined in.
        /// </item>
        /// <item>An empty string.</item>
        /// </list>
        /// </returns>
        protected static string GetAssemblyDescription() =>
            GetAssemblyDescription(typeof(TExecutor));

        /// <inheritdoc cref="GetAssemblyDescription()"/>
        protected static string GetAssemblyDescription(Type executorType)
        {
            _ = executorType ?? throw new ArgumentNullException(nameof(executorType));

            Assembly programAssembly = executorType.Assembly;
            string? description = programAssembly
                .GetCustomAttribute<AssemblyDescriptionAttribute>()?
                .Description;
            if (string.IsNullOrEmpty(description))
                description = programAssembly
                    .GetCustomAttribute<AssemblyProductAttribute>()?
                    .Product;
            if (string.IsNullOrEmpty(description))
                description = executorType?.Namespace;
            return description ?? string.Empty;
        }

        /// <summary>
        /// Gets a command handler instance for the specified executor type.
        /// </summary>
        protected virtual ICommandHandler CommandHandler { get; } = CommandHandlerFactory.Create(
        async (InvocationContext context, IHost host) =>
        {
            using var serviceScope = host.Services.CreateScope();
            var targetInstance = ActivatorUtilities
                .GetServiceOrCreateInstance<TExecutor>(
                    serviceScope.ServiceProvider);
            var targetCommandHandler = CommandHandlerFactory
                .Create<CancellationToken>(targetInstance.RunAsync);

            return await targetCommandHandler.InvokeAsync(context)
                .ConfigureAwait(continueOnCapturedContext: false);
        });
    }
}
