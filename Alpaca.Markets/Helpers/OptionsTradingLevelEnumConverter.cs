namespace Alpaca.Markets;

[SuppressMessage(
	"Microsoft.Performance", "CA1812:Avoid uninstantiated internal classes",
	Justification = "Object instances of this class will be created by Newtonsoft.JSON library.")]
internal sealed class OptionsTradingLevelEnumConverter : JsonConverter
{
	public override bool CanConvert(Type objectType)
	{
		return objectType == typeof(OptionsTradingLevel) ||
			   objectType == typeof(OptionsTradingLevel?);
	}

	public override object? ReadJson(
		JsonReader reader,
		Type objectType,
		object? existingValue,
		JsonSerializer serializer)
	{
		if (reader.TokenType == JsonToken.Null)
		{
			return null;
		}

		// Handle numeric values
		if (reader.TokenType == JsonToken.Integer)
		{
			if (reader.Value is long intValue)
			{
				return (OptionsTradingLevel)intValue;
			}
		}

		// Handle string values
		if (reader.TokenType == JsonToken.String)
		{
			var stringValue = reader.Value?.ToString();

			// Try to parse as number first
			if (int.TryParse(stringValue, out int numericValue))
			{
				return (OptionsTradingLevel)numericValue;
			}

			if (stringValue != null && Enum.TryParse<OptionsTradingLevel>(stringValue, true, out var result))
			{
				return result;
			}
		}

		// Default to Disabled if unable to parse
		return OptionsTradingLevel.Disabled;
	}

	public override void WriteJson(
		JsonWriter writer,
		object? value,
		JsonSerializer serializer)
	{
		if (value == null)
		{
			writer.WriteNull();
			return;
		}

		// Write as integer
		writer.WriteValue((int)value);
	}
}