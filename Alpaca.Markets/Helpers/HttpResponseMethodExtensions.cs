using Alpaca.Markets.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

namespace Alpaca.Markets;

internal static class HttpResponseMethodExtensions
{
	// Create a standardized JsonSerializerSettings to use throughout the application
	[SuppressMessage("Code Quality", "CS0618:Type or member is obsolete")]
	private static readonly JsonSerializerSettings StandardSerializerSettings = new JsonSerializerSettings
	{
		DateParseHandling = DateParseHandling.None,
		Culture = CultureInfo.InvariantCulture,
		ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
		MissingMemberHandling = MissingMemberHandling.Ignore,
		NullValueHandling = NullValueHandling.Ignore,
		DefaultValueHandling = DefaultValueHandling.Populate,
		// Enable these additional settings for more flexible deserialization
		TypeNameHandling = TypeNameHandling.None,
		MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
		StringEscapeHandling = StringEscapeHandling.EscapeHtml,
		CheckAdditionalContent = false, // Continue even if there's content after JSON
		DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
		MaxDepth = 128, // Increase from default if you have deeply nested objects
		ContractResolver = new DefaultContractResolver
		{
			// Make properties writable
#pragma warning disable CS0618 // Type or member is obsolete
			DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Default
#pragma warning restore CS0618 // Type or member is obsolete
		},
		Converters = new List<JsonConverter>
		{
			new OptionsTradingLevelEnumConverter(),
			new UnixSecondsDateTimeConverter(),
			new TrimAwareDecimalConverter(),
			new DecimalListConverter(),
			new AccountActivityTypeEnumConverter(),
			new StringEnumConverter(),
			new AssetAttributesEnumConverter(),
			new CryptoExchangeEnumConverter(),
			new ExchangeEnumConverter(),
			new OrderSideEnumConverter(),
			new AssumeUtcIsoDateTimeConverter(),
			new DateOnlyConverter(),
			new TimeOnlyConverter(),
			new AssetAttributesEnumConverter()
		},
		Error = (sender, args) => {
			System.Diagnostics.Debug.WriteLine($"Error deserializing: {args.ErrorContext.Error}");
			System.Diagnostics.Debug.WriteLine($"Member: {args.ErrorContext.Member}");
			System.Diagnostics.Debug.WriteLine($"Path: {args.ErrorContext.Path}");

			// Only handle specific errors to avoid masking serious issues
			if (args.ErrorContext.Error is JsonSerializationException)
			{
				args.ErrorContext.Handled = true; // Try to continue
			}
		}
	};

	// Custom converter for handling trimmed decimal values
	public sealed class TrimAwareDecimalConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(decimal) || objectType == typeof(decimal?);
		}

		public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return objectType == typeof(decimal?) ? (decimal?)null : 0m;
			}

			// Handle strings that might be trimmed
			if (reader.TokenType == JsonToken.String)
			{
				var value = reader.Value?.ToString();
				if (value != null && decimal.TryParse(value, out decimal result))
				{
					return result;
				}
			}

			return reader.Value is decimal dec ? dec : 0m;
		}

		public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
		{
			writer.WriteValue(value);
		}
	}

	[SuppressMessage("Code Quality", "CS0618:Type or member is obsolete")]
	public static async Task<TApi> DeserializeAsync<TApi, TJson>(
		this HttpResponseMessage response)
		where TJson : TApi
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(response);
#else
		if (response == null)
			throw new ArgumentNullException(nameof(response));
#endif

		// For direct string deserialization (gives better error context)
		var rawJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

		if (response.IsSuccessStatusCode)
		{
			try
			{
				// Check if the JSON starts with an array or object
				var firstNonWhitespaceChar = rawJson.TrimStart()[0];

				if (firstNonWhitespaceChar == '[')
				{
					// Handle array JSON
					var jArray = JArray.Parse(rawJson);
					var serializer = JsonSerializer.Create(StandardSerializerSettings);

					// If the expected type is a collection/list
					if (typeof(TApi).IsGenericType &&
						(typeof(IEnumerable<>).IsAssignableFrom(typeof(TApi).GetGenericTypeDefinition()) ||
						 typeof(ICollection<>).IsAssignableFrom(typeof(TApi).GetGenericTypeDefinition())))
					{
						using (var jsonTokenReader = new JTokenReader(jArray))
						{
							var result = (TJson)serializer.Deserialize(jsonTokenReader, typeof(TJson))!;
							if (result == null)
							{
								throw new RestClientErrorException("Unable to deserialize JSON array response.");
							}
							return result;
						}
					}
				}
				else if (firstNonWhitespaceChar == '{')
				{
					// Current implementation for object JSON
					var jObject = JObject.Parse(rawJson);
					var serializer = JsonSerializer.Create(StandardSerializerSettings);
					using (var jsonTokenReader = new JTokenReader(jObject))
					{
						var result = (TJson)serializer.Deserialize(jsonTokenReader, typeof(TJson))!;
						if (result == null)
						{
							throw new RestClientErrorException("Unable to deserialize JSON response message.");
						}
						return result;
					}
				}

				// Fallback to direct deserialization if above approaches don't match
				var fallbackResult = JsonConvert.DeserializeObject<TJson>(rawJson, StandardSerializerSettings);
				if (fallbackResult == null)
				{
					throw new RestClientErrorException("Unable to deserialize JSON response message.");
				}
				return fallbackResult;
			}
			catch (JsonException jsonEx) // Catches JsonReaderException, JsonSerializationException, etc.
			{
				// Log both the exception and a preview of the JSON for diagnostic purposes
				var jsonPreview = rawJson.Length > 100
					? rawJson[..100] + "..."
					: rawJson;

				throw new RestClientErrorException(
					$"JSON parsing error: {jsonEx.Message}. JSON preview: {jsonPreview}", jsonEx);
			}
			catch (InvalidOperationException invOpEx)
			{
				throw new RestClientErrorException(
					$"Invalid operation during deserialization: {invOpEx.Message}. JSON: {rawJson}", invOpEx);
			}
			catch (ArgumentException argEx)
			{
				throw new RestClientErrorException(
					$"Invalid argument during deserialization: {argEx.Message}. JSON: {rawJson}", argEx);
			}
			// All other exceptions will be rethrown as is
		}

		// For error responses, throw the appropriate exception
#pragma warning disable CS0612 // Type or member is obsolete
		throw processErrorResponse(response, rawJson);
#pragma warning restore CS0612 // Type or member is obsolete
	}

	// Alternative deserialization method using streaming for large payloads
	[Obsolete]
	public static async Task<TApi> DeserializeStreamingAsync<TApi, TJson>(
		this HttpResponseMessage response)
		where TJson : TApi
	{
#if NET6_0_OR_GREATER
		ArgumentNullException.ThrowIfNull(response);
#else
		if (response == null)
			throw new ArgumentNullException(nameof(response));
#endif

		if (response.IsSuccessStatusCode)
		{
			try
			{
#if NETSTANDARD2_1 || NET6_0_OR_GREATER
				var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
				await using var _ = stream.ConfigureAwait(false);
#else
				using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
				using var reader = new StreamReader(stream);
				using var jsonReader = new JsonTextReader(reader);

				var serializer = JsonSerializer.Create(StandardSerializerSettings);
				var result = serializer.Deserialize<TJson>(jsonReader);

				if (result == null)
				{
					throw new RestClientErrorException("Unable to deserialize JSON response message.");
				}

				return result;
			}
			catch (JsonException jsonEx)
			{
				// In streaming mode, we need to read the content again for error reporting
				var rawJson = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
				var jsonPreview = rawJson.Length > 100
					? rawJson[..100] + "..."
					: rawJson;

				throw new RestClientErrorException(
					$"JSON parsing error during streaming: {jsonEx.Message}. JSON preview: {jsonPreview}", jsonEx);
			}
		}

		// For error responses, get the content as string and process
		var errorContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		throw processErrorResponse(response, errorContent);
	}

	[Obsolete]
	private static RestClientErrorException processErrorResponse(
		HttpResponseMessage response,
		string rawJson)
	{
		try
		{
			var jsonError = JsonConvert.DeserializeObject<JsonError>(rawJson, StandardSerializerSettings) ?? new JsonError();
			jsonError.Code ??= (Int32)response.StatusCode;

			return String.IsNullOrEmpty(jsonError.Message)
				? new RestClientErrorException(response)
				: new RestClientErrorException(response, jsonError);
		}
		catch (JsonReaderException)
		{
			// For JSON reader errors, we already have the raw JSON
			return new RestClientErrorException(
				$"Invalid JSON in error response. Status: {response.StatusCode}. Content: {rawJson}");
		}
		catch (JsonSerializationException)
		{
			// For JSON serialization errors, we already have the raw JSON
			return new RestClientErrorException(
				$"Unable to deserialize error response. Status: {response.StatusCode}. Content: {rawJson}");
		}
		// No general catch - for any other exceptions, let them propagate up
	}

	public static async Task<Boolean> IsSuccessStatusCodeAsync(
		this HttpResponseMessage response)
	{
		if (response.IsSuccessStatusCode)
		{
			return true;
		}
#if NETSTANDARD2_1 || NET6_0_OR_GREATER
		var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
		await using var _ = stream.ConfigureAwait(false);
#else
        using var stream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
#endif
		// ReSharper disable once UseAwaitUsing
		using var reader = new JsonTextReader(new StreamReader(stream));
		throw getException(response, reader);
	}

	[SuppressMessage(
		"Design", "CA1031:Do not catch general exception types",
		Justification = "We wrap all exceptions into the single exception type.")]
	private static RestClientErrorException getException(
		HttpResponseMessage response,
		JsonReader reader)
	{
		try
		{
			var jsonError = new JsonSerializer()
				.Deserialize<JsonError>(reader) ?? new JsonError();
			jsonError.Code ??= (Int32)response.StatusCode;
			return String.IsNullOrEmpty(jsonError.Message)
				? new RestClientErrorException(response)
				: new RestClientErrorException(response, jsonError);
		}
		catch (JsonReaderException)
		{
			return new RestClientErrorException(response);
		}
		catch (Exception exception)
		{
			return new RestClientErrorException(response, exception);
		}
	}
}