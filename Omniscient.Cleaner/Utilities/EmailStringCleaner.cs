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
        string delimiter = Environment.NewLine + Environment.NewLine;
        int index = content.IndexOf(delimiter, StringComparison.Ordinal);
        if (index >= 0)
        {
            return content.Substring(index + delimiter.Length);
        }

        return content;
    }
}