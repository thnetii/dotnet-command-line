using System;
using System.CommandLine;
using System.CommandLine.Hosting;
using System.CommandLine.Invocation;
using System.Reflection;
using System.Threading;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace THNETII.CommandLine.Hosting
{
    /// <summary>
    /// Provides convenience methods for creating instances of
    /// <see cref="IHostBuilder"/> using pre-configures defaults, including
    /// additional defaults that are suited for command-line applications.
    /// </summary>
    /// <seealso cref="Host"/>
    public static class CommandLineHost
    {
        /// <summary>
        /// Creates a new instance of the <see cref="HostBuilder"/> class using
        /// the defaults configured by <see cref="Host.CreateDefaultBuilder(string[])"/>.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <remarks>
        /// Additionally added pre-configured defaults for command-line applications:
        /// <list type="bullet">
        /// <item>load app <see cref="IConfiguration"/> from embedded <c>appsettings.json</c> and <c>appsettings.[<see cref="IHostEnvironment.EnvironmentName"/>].json</c> <strong>before</strong> the configuration is loaded from the physical file system.</item>
        /// <item>Add the <see cref="InvocationLifetimeOptions"/> type as an option service to the DI-service registration and configure it to be bound from the <see cref="IConfiguration"/> section named <c>Lifetime</c>.</item>
        /// </list>
        /// </remarks>
        public static IHostBuilder CreateDefaultBuilder(string[]? args)
        {
            return Host.CreateDefaultBuilder(args ?? Array.Empty<string>())
                .ConfigureEmbeddedAppConfiguration(Assembly.GetEntryAssembly()!)
                .ConfigureCommandLineInvocation();
        }

        internal static Func<string[], IHostBuilder> DefaultBuilderFactory { get; } =
            CreateDefaultBuilder;

        /// <summary>
        /// Gets a descriptive string for use in the
        /// <see cref="Symbol.Description"/> property of the
        /// <see cref="RootCommand"/> for a command-line application.
        /// </summary>
        /// <param name="assembly">The application entry assembly.</param>
        /// <returns>
        /// The first of the following that is not <see langword="null"/> or an
        /// empty string.
        /// <list type="number">
        /// <item>
        /// The <see cref="AssemblyDescriptionAttribute.Description"/> property
        /// of the <see cref="AssemblyDescriptionAttribute"/> declared on the
        /// passed assembly (if available).
        /// </item>
        /// <item>
        /// The <see cref="AssemblyProductAttribute.Product"/> property
        /// of the <see cref="AssemblyProductAttribute"/> declared on the
        /// passed assembly (if available).
        /// </item>
        /// <item>
        /// The <see cref="Type.Namespace"/> of the type declaring the entry
        /// point of the passed assembly (if available).
        /// </item>
        /// </list>
        /// If none of the above is available, an empty string is returned.
        /// </returns>
        public static string GetAssemblyDescription(Assembly? assembly)
        {
            if (assembly is null)
                return string.Empty;
            return GetAssemblyDescription(assembly, assembly.EntryPoint?.DeclaringType);
        }

        /// <inheritdoc cref="GetAssemblyDescription(Assembly?)"/>
        /// <param name="type">The type containing the assembly.</param>
        public static string GetAssemblyDescription(Type? type)
        {
            if (type is null)
                return string.Empty;
            return GetAssemblyDescription(type.Assembly, type);
        }

        private static string GetAssemblyDescription(Assembly assembly, Type? type)
        {
            string? description = assembly.GetCustomAttribute
                <AssemblyDescriptionAttribute>()?.Description;
            if (string.IsNullOrEmpty(description))
                description = assembly.GetCustomAttribute
                    <AssemblyProductAttribute>()?.Product;
            if (string.IsNullOrEmpty(description) && !(type is null))
                description = type.Namespace;
            return description ?? string.Empty;
        }

        /// <inheritdoc cref="GetAssemblyDescription(Assembly?)"/>
        public static string GetEntryAssemblyDescription() =>
            GetAssemblyDescription(Assembly.GetEntryAssembly());

        /// <inheritdoc cref="GetAssemblyDescription(Type?)"/>
        public static string GetAssemblyDescription<T>() =>
            GetAssemblyDescription(typeof(T));

        /// <summary>
        /// Creates a Command handler for the specified
        /// command-line executor type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The command-line executor type to construct and invoke.
        /// </typeparam>
        /// <returns>
        /// A <see cref="ICommandHandler"/> that binds a configured
        /// <see cref="IHost"/> parameter. The <see cref="IHost.Services"/>
        /// DI-container is used to create an instance of
        /// <typeparamref name="T"/>. Finally,
        /// <see cref="ICommandLineExecutor.RunAsync(CancellationToken)"/> is
        /// invoked to handle the command.
        /// </returns>
        public static ICommandHandler GetCommandHandler<T>()
            where T : ICommandLineExecutor
        {
            return CommandHandler.Create(
            async (InvocationContext context, IHost host) =>
            {
                using var serviceScope = host.Services.CreateScope();
                var serviceProvider = serviceScope.ServiceProvider;

                var targetInstance = ActivatorUtilities
                    .GetServiceOrCreateInstance<T>(serviceProvider);
                var targetHandler = CommandHandler.Create<CancellationToken>(
                    targetInstance.RunAsync);

                return await targetHandler.InvokeAsync(context)
                    .ConfigureAwait(continueOnCapturedContext: false);
            });
        }
    }
}
