using System.Buffers;
using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Kapusch.TikTok.Business.Android.Internal;

/// <summary>
/// Serializes event properties to the JSON object the native facade turns into an
/// <c>org.json.JSONObject</c>. Uses <see cref="Utf8JsonWriter"/> directly so it stays
/// trimming and AOT safe without reflection-based serialization.
/// </summary>
internal static class TikTokBusinessJson
{
	internal static string? Serialize(IReadOnlyDictionary<string, object>? properties)
	{
		if (properties is null || properties.Count == 0)
		{
			return null;
		}

		var buffer = new ArrayBufferWriter<byte>();
		using (var writer = new Utf8JsonWriter(buffer))
		{
			writer.WriteStartObject();
			foreach (var (key, value) in properties)
			{
				if (string.IsNullOrWhiteSpace(key))
				{
					throw new ArgumentException("Event property names must not be empty.", nameof(properties));
				}
				if (value is null)
				{
					continue;
				}
				writer.WritePropertyName(key);
				WriteValue(writer, value, key);
			}
			writer.WriteEndObject();
		}
		return Encoding.UTF8.GetString(buffer.WrittenSpan);
	}

	private static void WriteValue(Utf8JsonWriter writer, object value, string path)
	{
		switch (value)
		{
			case string text:
				writer.WriteStringValue(text);
				return;
			case bool flag:
				writer.WriteBooleanValue(flag);
				return;
			case byte or sbyte or short or ushort or int or uint or long:
				writer.WriteNumberValue(Convert.ToInt64(value, CultureInfo.InvariantCulture));
				return;
			case ulong unsigned:
				writer.WriteNumberValue(unsigned);
				return;
			case decimal number:
				writer.WriteNumberValue(number);
				return;
			case float single:
				WriteFinite(writer, single, path);
				return;
			case double real:
				WriteFinite(writer, real, path);
				return;
			case TikTokBusinessContent content:
				WriteContent(writer, content);
				return;
			case IReadOnlyDictionary<string, object> map:
				WriteObject(writer, map, path);
				return;
			case IDictionary<string, object> map:
				WriteObject(writer, new Dictionary<string, object>(map), path);
				return;
			case IEnumerable sequence:
				writer.WriteStartArray();
				foreach (var item in sequence)
				{
					if (item is null)
					{
						throw new ArgumentException($"Event property '{path}' contains a null array item.");
					}
					WriteValue(writer, item, path);
				}
				writer.WriteEndArray();
				return;
			default:
				throw new ArgumentException(
					$"Event property '{path}' has unsupported type {value.GetType().FullName}. "
						+ "Use string, bool, a number, TikTokBusinessContent, a dictionary or a list."
				);
		}
	}

	private static void WriteFinite(Utf8JsonWriter writer, double value, string path)
	{
		if (!double.IsFinite(value))
		{
			throw new ArgumentException($"Event property '{path}' must be a finite number.");
		}
		writer.WriteNumberValue(value);
	}

	private static void WriteObject(Utf8JsonWriter writer, IReadOnlyDictionary<string, object> map, string path)
	{
		writer.WriteStartObject();
		foreach (var (key, value) in map)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				throw new ArgumentException($"Event property '{path}' contains an empty name.");
			}
			if (value is null)
			{
				continue;
			}
			writer.WritePropertyName(key);
			WriteValue(writer, value, path + "." + key);
		}
		writer.WriteEndObject();
	}

	// Mirrors com.tiktok.appevents.contents.TTContentParams#toJSONObject, which sends
	// the price as a string.
	private static void WriteContent(Utf8JsonWriter writer, TikTokBusinessContent content)
	{
		writer.WriteStartObject();
		if (content.Quantity is { } quantity)
		{
			writer.WriteNumber(TikTokBusinessPropertyNames.ContentQuantity, quantity);
		}
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentId, content.ContentId);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentCategory, content.ContentCategory);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentName, content.ContentName);
		WriteOptional(writer, TikTokBusinessPropertyNames.ContentBrand, content.Brand);
		if (content.Price is { } price)
		{
			writer.WriteString(TikTokBusinessPropertyNames.ContentPrice, price.ToString(CultureInfo.InvariantCulture));
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
