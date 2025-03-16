using System.Text.RegularExpressions;
using Omniscient.ServiceDefaults;

namespace Omniscient.Cleaner.Utilities;

public static class EmailStringCleaner
{
    /// <summary>
    /// Removes the email headers from the content.
    /// </summary>
    /// <param name="content">The complete content of the email file including headers.</param>
    /// <returns>The content without the headers.</returns>
    public static string RemoveHeaders(string content)
    {
        using var activity = ActivitySources.OmniscientActivitySource.StartActivity();

        var pattern = @"^(?:(?!\r?\n\r?\n).)*\r?\n\r?\n";
        var regex = new Regex(pattern, RegexOptions.Singleline);

        var match = regex.Match(content);
        if (match.Success)
        {
            return content.Substring(match.Length).TrimStart();
        }

        return content;
    }

}