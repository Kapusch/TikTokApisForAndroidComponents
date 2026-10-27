using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Kapusch.TikTok.Business.Android.Internal;

/// <summary>
/// Serializes event properties to the JSON object the native facade turns into an
/// <c>org.json.JSONObject</c>. Uses <see cref="Utf8JsonWriter"/> directly so it stays
/// trimming and AOT safe without reflection-based serialization. Accepts the same value types
/// as the iOS package.
/// </summary>
internal static class TikTokBusinessJson
{
	private const int MaxDepth = 8;

	internal static string? WriteProperties(IReadOnlyDictionary<string, object>? properties)
	{
		if (properties is null || properties.Count == 0)
		{
			return null;
		}

		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			WriteObject(
				writer,
				properties.Select(static pair => new KeyValuePair<string, object?>(pair.Key, pair.Value)),
				depth: 0
			);
		}
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	internal static string FormatDecimal(decimal value) =>
		value.ToString("0.############################", CultureInfo.InvariantCulture);

	private static void WriteObject(
		Utf8JsonWriter writer,
		IEnumerable<KeyValuePair<string, object?>> properties,
		int depth
	)
	{
		writer.WriteStartObject();
		foreach (var (key, value) in properties)
		{
			ArgumentException.ThrowIfNullOrEmpty(key, nameof(properties));
			writer.WritePropertyName(key);
			WriteValue(writer, value, depth + 1);
		}
		writer.WriteEndObject();
	}

	private static void WriteValue(Utf8JsonWriter writer, object? value, int depth)
	{
		if (depth > MaxDepth)
		{
			throw new ArgumentException("TikTok event properties are nested too deeply.");
		}

		switch (value)
		{
			case null:
				writer.WriteNullValue();
				break;
			case string text:
				writer.WriteStringValue(text);
				break;
			case bool flag:
				writer.WriteBooleanValue(flag);
				break;
			case decimal number:
				writer.WriteNumberValue(number);
				break;
			case double number when double.IsFinite(number):
				writer.WriteNumberValue(number);
				break;
			case float number when float.IsFinite(number):
				writer.WriteNumberValue(number);
				break;
			case int or long or short or byte or sbyte or ushort or uint:
				writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
				break;
			case ulong number:
				writer.WriteNumberValue(number);
				break;
			case DateTimeOffset timestamp:
				writer.WriteStringValue(timestamp.ToString("O", CultureInfo.InvariantCulture));
				break;
			case DateTime timestamp:
				writer.WriteStringValue(timestamp.ToString("O", CultureInfo.InvariantCulture));
				break;
			case TikTokBusinessContent content:
				WriteContent(writer, content);
				break;
			case IEnumerable<KeyValuePair<string, object?>> nested:
				WriteObject(writer, nested, depth);
				break;
			case IDictionary dictionary:
				writer.WriteStartObject();
				foreach (DictionaryEntry entry in dictionary)
				{
					var key = Convert.ToString(entry.Key, CultureInfo.InvariantCulture);
					if (string.IsNullOrEmpty(key))
					{
						throw new ArgumentException("TikTok event property keys must be non-empty strings.");
					}
					writer.WritePropertyName(key);
					WriteValue(writer, entry.Value, depth + 1);
				}
				writer.WriteEndObject();
				break;
			case IEnumerable items:
				writer.WriteStartArray();
				foreach (var item in items)
				{
					WriteValue(writer, item, depth + 1);
				}
				writer.WriteEndArray();
				break;
			default:
				throw new ArgumentException(
					$"Unsupported TikTok event property value type '{value.GetType().FullName}'. "
						+ "Use strings, booleans, numbers, dictionaries, lists or TikTokBusinessContent."
				);
		}
	}

	// Mirrors com.tiktok.appevents.contents.TTContentParams#toJSONObject: quantity is a
	// number, the price a string, and an unset price is omitted.
	private static void WriteContent(Utf8JsonWriter writer, TikTokBusinessContent content)
	{
		writer.WriteStartObject();
		writer.WriteNumber(TikTokBusinessPropertyNames.Quantity, content.Quantity);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentId, content.ContentId);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentCategory, content.ContentCategory);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentName, content.ContentName);
		WriteOptional(writer, TikTokBusinessPropertyNames.Brand, content.Brand);
		if (content.Price is { } price)
		{
			writer.WriteString(TikTokBusinessPropertyNames.Price, FormatDecimal(price));
		}
		writer.WriteEndObject();
	}

	private static void WriteOptional(Utf8JsonWriter writer, string name, string? value)
	{
		if (!string.IsNullOrEmpty(value))
		{
			writer.WriteString(name, value);
		}
	}
}
