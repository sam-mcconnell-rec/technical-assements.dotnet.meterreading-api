namespace MeterReadingApi.RestfulAPI.Extensions;

public static class FormFileExtensions
{
    /// <summary>
    /// Read a IFormFile line by line until the end of the file.
    /// </summary>
    /// <param name="file"></param>
    /// <returns></returns>
    public static async IAsyncEnumerable<string?> ReadLinesFromFile(this IFormFile file)
    {
        using var reader = new StreamReader(file.OpenReadStream());
        while (reader.Peek() >= 0)
        {
            var line = await reader.ReadLineAsync();
            yield return line;
        }
    }

    /// <summary>
    /// Parses a CSV file into a target object using the loader provided. Blank lines are skipped. All errors are logged and returned at the end.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="file"></param>
    /// <param name="loader"></param>
    /// <param name="delimiter"></param>
    /// <param name="linesToSkipAtStart"></param>
    /// <returns></returns>
    public static async Task<(int skippedLines, List<T> output, List<string> errors)> ParseCvsFile<T>(this IFormFile file, Func<List<string>, (bool success, T? parsedObject, IEnumerable<string> errors)> loader, char delimiter = ',', int linesToSkipAtStart = 0)
    {
        var skippedLines = 0;
        var lineCount = 0;
        var output = new List<T>();
        var errors = new List<string>();
        await foreach (var line in file.ReadLinesFromFile().Skip(linesToSkipAtStart))
        {
            lineCount++;
            if (string.IsNullOrWhiteSpace(line))
            {
                skippedLines++;
                errors.Add($"Error parsing csv, line {lineCount}: Blank line");
                continue;
            }

            var parts = line.Split(delimiter);

            var result = loader.Invoke(parts.ToList());

            if (result.success && result.parsedObject != null)
            {
                output.Add(result.parsedObject!);
            }
            else
            {
                errors.AddRange(result.errors.Select(e => $"Error parsing csv, line {lineCount}: " + e));
                skippedLines++;
            }
        }
        return (skippedLines, output, errors);
    }

}