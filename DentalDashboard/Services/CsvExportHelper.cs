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

    public static string Quote(string? value)
    {
        var safeValue = PreventFormulaInjection(value ?? string.Empty);
        return $"\"{safeValue.Replace("\"", "\"\"")}\"";
    }

    private static string PreventFormulaInjection(string value)
    {
        var firstNonWhitespace = value.AsSpan().TrimStart();
        if (!firstNonWhitespace.IsEmpty && firstNonWhitespace[0] is '=' or '+' or '-' or '@')
            return "'" + value;

        return value;
    }

    public static string JoinRow(params string?[] values) =>
        string.Join(',', values.Select(Quote));
}
