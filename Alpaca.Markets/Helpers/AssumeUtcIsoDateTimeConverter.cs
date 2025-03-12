using System;
using System.Globalization;

namespace Alpaca.Markets;

[SuppressMessage(
    "Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
    Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
internal sealed class AssumeUtcIsoDateTimeConverter : DateTimeConverterBase
{
	public override void WriteJson(
		JsonWriter writer,
		Object? value,
		JsonSerializer serializer)
	{
		if (value is DateTime dateTimeValue)
		{
			writer.WriteValue(dateTimeValue.ToString("O"));
		}
		else
		{
			writer.WriteNull();
		}
	}

	public override Object? ReadJson(
		JsonReader reader,
		Type objectType,
		Object? existingValue,
		JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
		}

		if (reader.TokenType == JsonToken.String)
		{
			string? dateTimeString = reader.Value?.ToString();
			if (string.IsNullOrEmpty(dateTimeString))
			{
				return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
			}

			// Try parsing directly first (handles standard formats)
			if (DateTimeOffset.TryParse(dateTimeString,
				CultureInfo.InvariantCulture,
				DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
				out var dateTimeOffset))
			{
				return dateTimeOffset.UtcDateTime;
			}

			// If that fails, try truncating the nanoseconds to 7 digits (what .NET can handle)
			try
			{
				// Null check added here to satisfy CS8602
#pragma warning disable CA1508 // Type or member is obsolete
				if (dateTimeString != null)
#pragma warning restore CA1508 // Type or member is obsolete
				{
					// Extract parts before the decimal point - use StringComparison to satisfy CA1307
#if NET8_0
                    int decimalPointIndex = dateTimeString.IndexOf('.', StringComparison.Ordinal);
#else
					int decimalPointIndex = dateTimeString.IndexOf('.');
#endif

					if (decimalPointIndex >= 0)
					{
						// Find the position of 'Z' or other timezone indicator
						char[] timeZoneChars = new[] { 'Z', '+', '-' };
						int timezoneIndex = dateTimeString.LastIndexOfAny(timeZoneChars);

						if (timezoneIndex > decimalPointIndex)
						{
							// Calculate fraction length
							int fractionLength = timezoneIndex - (decimalPointIndex + 1);

							// Truncate to 7 digits if longer
							if (fractionLength > 7)
							{
								// Use substring for compatibility
								string beforeDecimal = dateTimeString.Substring(0, decimalPointIndex + 1);
								string fraction = dateTimeString.Substring(decimalPointIndex + 1, 7);
								string timezone = dateTimeString.Substring(timezoneIndex);

								// Construct the truncated string
								string truncatedDateTimeString = beforeDecimal + fraction + timezone;

								// Try parsing the truncated string
								if (DateTimeOffset.TryParse(truncatedDateTimeString,
									CultureInfo.InvariantCulture,
									DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
									out dateTimeOffset))
								{
									return dateTimeOffset.UtcDateTime;
								}
							}
						}
					}
				}

				// If all else fails and dateTimeString is not null, try direct parsing
				if (dateTimeString != null)
				{
					return DateTime.Parse(dateTimeString, CultureInfo.InvariantCulture,
						DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
				}
			}
			catch (FormatException)
			{
				// Specific exception for format issues
				return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
			}
			catch (OverflowException)
			{
				// Specific exception for overflow issues
				return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
			}
			// Let other exceptions propagate
		}

		// Default fallback for non-string tokens
		return objectType == typeof(DateTime?) ? null : DateTime.MinValue;
	}
}