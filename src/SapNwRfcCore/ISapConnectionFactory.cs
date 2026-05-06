namespace SapNwRfcCore;

/// <summary>
/// Factory to create a new SAP connection.
/// </summary>
public interface ISapConnectionFactory
{
    /// <summary>
    /// Creates a new SAP connection.
    /// </summary>
    /// <returns>A new instance of <see cref="ISapConnection"/>.</returns>
    ISapConnection CreateConnection();
}
