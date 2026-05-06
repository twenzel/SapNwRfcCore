using SapNwRfcCore;
using SapNwRfcCore.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extensions to add the RFC services to the <see cref="IServiceCollection"/>
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the RFC services to the <see cref="IServiceCollection"/> and configures the SAP connection parameters using the provided setup action.
    /// </summary>
    /// <param name="services">The service collection to which the SAP RFC connector services will be added. Cannot be null.</param>
    /// <param name="sapConnectionParametersSetup">An action to configure the SAP connection parameters. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddSapConnector(this IServiceCollection services, Action<SapConnectionParameters> sapConnectionParametersSetup)
    {
        var settings = new SapConnectionParameters();
        sapConnectionParametersSetup(settings);

        return AddSapConnector(services, settings);
    }

    /// <summary>
    /// Adds the RFC services to the <see cref="IServiceCollection"/> and configures the SAP connection parameters using the provided connection string.
    /// </summary>
    /// <param name="services">The service collection to which the SAP RFC connector services will be added. Cannot be null.</param>
    /// <param name="connectionString">The connection string used to configure the SAP connection parameters. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddSapConnector(this IServiceCollection services, string connectionString)
    {
        return AddSapConnector(services, SapConnectionParameters.Parse(connectionString));
    }

    /// <summary>
    /// Adds SAP RFC connector services and configuration to the specified service collection.
    /// </summary>
    /// <remarks>Registers the required services for SAP RFC connectivity, including connection factories and
    /// configuration. Call this method during application startup to enable SAP RFC integration via dependency
    /// injection.</remarks>
    /// <param name="services">The service collection to which the SAP RFC connector services will be added. Cannot be null.</param>
    /// <param name="sapConnectionParameters">The SAP connection parameters used to configure the RFC connector. Cannot be null.</param>
    /// <returns>The same service collection instance, enabling method chaining.</returns>
    public static IServiceCollection AddSapConnector(this IServiceCollection services, SapConnectionParameters sapConnectionParameters)
    {
        services.AddSingleton(sapConnectionParameters);
        services.AddSingleton<ISapConnectionFactory, SapConnectionFactory>();
        services.AddTransient(sp => sp.GetRequiredService<ISapConnectionFactory>().CreateConnection());

        return services;
    }
}
