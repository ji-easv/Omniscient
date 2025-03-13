using Microsoft.Extensions.Configuration;

namespace Omniscient.ServiceDefaults;

public static class EnvironmentHelper
{
    /// <summary>
    /// Gets a value from environment variables or configuration
    /// </summary>
    /// <param name="key">The key to look for</param>
    /// <param name="configuration">Optional configuration to check if environment variable is not found</param>
    /// <param name="configKey"></param>
    /// <returns>The value from environment variables or configuration</returns>
    /// <exception cref="Exception">Thrown when neither source contains the key</exception>
    public static string GetValue(string key, IConfiguration? configuration = null, string? configKey = null)
    {
        // Try environment variable first
        var value = Environment.GetEnvironmentVariable(key);
        
        // If not found in environment, try configuration
        if (string.IsNullOrEmpty(value) && configuration != null)
        {
            value = configuration[configKey ?? key];
        }
        
        // Throw if not found in either place
        return value ?? throw new Exception($"Value for '{key}' not found in environment variables or configuration");
    }
}