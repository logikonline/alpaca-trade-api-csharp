using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace Alpaca.Markets
{
	internal sealed class AssetAttributesListConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(List<AssetAttributes>);
		}

		public override object? ReadJson(
			JsonReader reader,
			Type objectType,
			object? existingValue,
			JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return new List<AssetAttributes>();

			var result = new List<AssetAttributes>();

			if (reader.TokenType != JsonToken.StartArray)
				throw new JsonSerializationException($"Unexpected token {reader.TokenType} when parsing attributes");

			var enumConverter = new AssetAttributesEnumConverter();

			while (reader.Read() && reader.TokenType != JsonToken.EndArray)
			{
				// Use your existing enum converter to convert each string
				var attribute = enumConverter.ReadJson(
					reader,
					typeof(AssetAttributes),
					null,
					serializer);

				if (attribute != null)
					result.Add((AssetAttributes)attribute);
			}

			return result;
		}

		public override void WriteJson(
			JsonWriter writer,
			object? value,
			JsonSerializer serializer)
		{
			var attributes = value as List<AssetAttributes> ?? new List<AssetAttributes>();

			writer.WriteStartArray();

			foreach (var attribute in attributes)
			{
				writer.WriteValue(attribute.ToEnumString());
			}

			writer.WriteEndArray();
		}
	}
}