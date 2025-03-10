using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace Alpaca.Markets.Helpers
{
	internal sealed class DecimalListConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(List<decimal?>) || objectType == typeof(List<decimal>);
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;

			if (reader.TokenType != JsonToken.StartArray)
			{
				throw new JsonSerializationException($"Expected array but got {reader.TokenType}");
			}

			var list = new List<decimal?>();

			while (reader.Read())
			{
				if (reader.TokenType == JsonToken.EndArray)
					break;

				if (reader.TokenType == JsonToken.Null)
				{
					list.Add(null);
					continue;
				}

				if (reader.TokenType == JsonToken.Float || reader.TokenType == JsonToken.Integer)
				{
					list.Add(Convert.ToDecimal(reader.Value, CultureInfo.InvariantCulture));
					continue;
				}

				if (reader.TokenType == JsonToken.String)
				{
					var value = reader.Value?.ToString();
					if (value != null && decimal.TryParse(value, out decimal result))
					{
						list.Add(result);
						continue;
					}
				}

				// If we can't parse it, add null
				list.Add(null);
			}

			return list;
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			if (value == null)
			{
				writer.WriteNull();
				return;
			}

			var list = (IEnumerable<decimal?>)value;

			writer.WriteStartArray();
			foreach (var item in list)
			{
				if (item.HasValue)
					writer.WriteValue(item.Value);
				else
					writer.WriteNull();
			}
			writer.WriteEndArray();
		}
	}
}
