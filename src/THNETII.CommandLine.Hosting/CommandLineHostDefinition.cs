using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// An abstract base class where all Symbols of a command can be stored
    /// together in oder to simplify Command-line argument binding for
    /// .NET Generic Host scenarios.
    /// </summary>
    public abstract class CommandLineHostDefinition
    {
        private readonly List<CommandLineHostDefinition> subCommandDefinitionList =
            new List<CommandLineHostDefinition>();

        /// <summary>
        /// The type that defining the method that is invoked for the
        /// <see cref="ICommandHandler"/> assigned to the
        /// <see cref="Command.Handler"/> property of <see cref="Command"/>.
        /// </summary>
        protected virtual Type? ExecutorType { get; }

        /// <summary>
        /// Creates a new command-line definition instance that will search the
        /// specified type for a method to invoke as the definition's
        /// command handler.
        /// </summary>
        /// <param name="executorType">A type definining the method for the command handler.</param>
        public CommandLineHostDefinition(Type? executorType)
        {
            ExecutorType = executorType;
        }

        /// <summary>
        /// Gets the <see cref="Command"/> that is definined by this instance.
        /// </summary>
        public abstract Command Command { get; }

        internal Type? GetExecutorType() => ExecutorType;

        /// <summary>
        /// Gets a list of definitions for the sub-commands that have been
        /// added to the <see cref="Command"/> property.
        /// </summary>
        /// <seealso cref="AddSubCommandDefinition(CommandLineHostDefinition)"/>
        public IEnumerable<CommandLineHostDefinition> SubCommandDefinitions =>
            subCommandDefinitionList;

        /// <summary>
        /// Adds a command definition as a sub-command to the command defined by
        /// the current instance. The <see cref="Command"/> property of
        /// <paramref name="definition"/> is added using
        /// <see cref="Command.AddCommand(Command)"/>.
        /// </summary>
        /// <param name="definition">
        /// A command definition instance defining the sub-command to add.
        /// </param>
        public void AddSubCommandDefinition(CommandLineHostDefinition definition)
        {
            _ = definition ?? throw new ArgumentNullException(nameof(definition));

            if (definition.Command is Command subCommand)
                Command.AddCommand(subCommand);
            subCommandDefinitionList.Add(definition);
        }

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
        /// <param name="executorType">The executor resposible for executing the command.</param>
        /// <returns>
        /// Attempts to find an appropriate description string using the
        /// following sources in order. The first non-empty string is returned:
        /// <list type="number">
        /// <item>
        /// The <see cref="AssemblyDescriptionAttribute.Description"/> property
        /// of the <see cref="AssemblyDescriptionAttribute"/> declared on the
        /// assembly containing <paramref name="executorType"/>.
        /// </item>
        /// <item>
        /// The <see cref="AssemblyProductAttribute.Product"/> property
        /// of the <see cref="AssemblyProductAttribute"/> declared on the
        /// assembly containing <paramref name="executorType"/>.
        /// </item>
        /// <item>
        /// The <see langword="namespace"/> that
        /// <paramref name="executorType"/> is defined in.
        /// </item>
        /// <item>An empty string.</item>
        /// </list>
        /// </returns>
        protected static string GetAssemblyDescription(Type? executorType)
        {
            string? description = null;
            if ((executorType?.Assembly ?? Assembly.GetEntryAssembly()) is Assembly programAssembly)
            {
                description = programAssembly
                    .GetCustomAttribute<AssemblyDescriptionAttribute>()?
                    .Description;
                if (string.IsNullOrEmpty(description))
                    description = programAssembly
                        .GetCustomAttribute<AssemblyProductAttribute>()?
                        .Product;
            }
            if (string.IsNullOrEmpty(description))
                description = executorType?.Namespace;
            return description ?? string.Empty;
        }

        /// <summary>
        /// Returns a command handler instance for the specified executor type.
        /// <para>
        /// Probes the type for a method named either <c>RunAsync</c> or <c>Run</c>.
        /// </para>
        /// </summary>
        /// <param name="executorType">The executor type to probe.</param>
        /// <returns>
        /// A command handler that can be assigned to <see cref="Command.Handler"/>.
        /// </returns>
        /// <remarks>
        /// If the found method is an instance method, a wrapping command
        /// handler is created that creates an instance of
        /// <paramref name="executorType"/> using the .NET Generic Host service
        /// provider.
        /// </remarks>
        /// <exception cref="ArgumentException">
        /// The type specified in <paramref name="executorType"/> does not
        /// define an unambiguous method named either 'Run' or 'RunAsync'.
        /// </exception>
        [return: NotNullIfNotNull("executorType")]
        protected static ICommandHandler? GetCommandHandler(Type? executorType)
        {
            if (executorType is null)
                return null;

            const BindingFlags bflags = BindingFlags.Public |
                BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.IgnoreCase;
            var runMethodInfo =
                executorType.GetMethod("RunAsync", bflags) ??
                executorType.GetMethod("Run", bflags);

            if (runMethodInfo is null)
                throw new ArgumentException(paramName: nameof(executorType),
                    message: $"The specified type '{executorType}' does not define an unambiguous method named either 'Run' or 'RunAsync'."
                    );

            if (runMethodInfo.IsStatic)
                return CommandHandler.Create(runMethodInfo);

            return CommandHandler.Create(
            async (InvocationContext context, IHost host) =>
            {
                using var serviceScope = host.Services.CreateScope();
                var targetInstance = ActivatorUtilities.GetServiceOrCreateInstance(
                    serviceScope.ServiceProvider, executorType);
                var targetCommandHandler = CommandHandler
                    .Create(runMethodInfo, targetInstance);

                return await targetCommandHandler.InvokeAsync(context)
                    .ConfigureAwait(continueOnCapturedContext: false);
            });
        }
    }
}
