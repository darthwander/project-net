using System.Globalization;

namespace EnrichIped.DataInfrastructure.Extensions;

internal static class StringExtensions
{
	private const string EmptyDateString = "0000-00-00 00:00:00";
	private static readonly DateTime EmptyDate = new(1000, 1, 1);

	private static readonly string[] Formats =
	[
		"yyyy-MM-dd HH:mm:ss",
		"yyyy-MM-dd HH:mm",
		"yyyy-MM-dd"
	];

	internal static DateTime SanitizeDateString(this string? input)
	{
		if (string.IsNullOrEmpty(input)
			|| input == EmptyDateString) return EmptyDate;

		input = input.SanitizeString();

		if (string.IsNullOrEmpty(input)) return EmptyDate;

		if (DateTime.TryParseExact(
				input,
				Formats,
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out var result)
			|| DateTime.TryParse(
				input,
				CultureInfo.InvariantCulture,
				DateTimeStyles.None,
				out result))
			return result;

		return EmptyDate;
	}

	internal static string SanitizeString(this string? input)
	{
		if (string.IsNullOrEmpty(input)) return string.Empty;

		input = input.Replace("\0", "")
			.Replace("\r", " ")
			.Replace("\n", " ")
			.Trim();

		return input;
	}

	internal static int OnlyNumbers(this string? input)
	{
		if (string.IsNullOrEmpty(input)) return 0;

		input = input.SanitizeString();
		input = new string(input.Where(char.IsDigit).ToArray());
		_ = int.TryParse(input, out var number);
		return number;
	}
}