using System.Text;

namespace DentalDashboard.Services;

public static class CsvExportHelper
{
    public static byte[] BuildFile(params string[] lines)
    {
        var builder = new StringBuilder();
        foreach (var line in lines)
            builder.AppendLine(line);

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(builder.ToString())).ToArray();
    }

    public static string Quote(string? value) => $"\"{(value ?? string.Empty).Replace("\"", "\"\"")}\"";

    public static string JoinRow(params string?[] values) =>
        string.Join(',', values.Select(Quote));
}
