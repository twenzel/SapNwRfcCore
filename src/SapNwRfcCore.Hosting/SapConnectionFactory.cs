namespace SapNwRfcCore.Hosting;

/// <summary>
/// Implementation of the <see cref="ISapConnectionFactory"/>
/// </summary>
public class SapConnectionFactory : ISapConnectionFactory
{
    private readonly SapConnectionParameters _rfcConnectionParameters;

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    /// <param name="sapConnectionParameters">The connection parameters.</param>
    /// <exception cref="ArgumentNullException"></exception>
    public SapConnectionFactory(SapConnectionParameters sapConnectionParameters)
    {
        _rfcConnectionParameters = sapConnectionParameters ?? throw new ArgumentNullException(nameof(sapConnectionParameters));
    }

    /// <inheritdoc />
    public ISapConnection CreateConnection()
    {
        return new SapConnection(_rfcConnectionParameters);
    }
}
