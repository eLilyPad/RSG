using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace RSG.Nonogram;

public sealed class Vector2IDictionaryConverter<TValue> : JsonConverter<Dictionary<Vector2I, TValue>>
{
	private const string KeyProp = "key", ValueProp = "value";

	public override Dictionary<Vector2I, TValue> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException("Expected JSON array.");
		Dictionary<Vector2I, TValue> result = [];
		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndArray) return result;
			if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected object.");

			Vector2I? key = null;
			TValue? value = default;

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject) break;
				if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

				string prop = reader.GetString()!;
				reader.Read();

				switch (prop)
				{
					case KeyProp:
						key = JsonSerializer.Deserialize<Vector2I>(ref reader, options);
						break;
					case ValueProp:
						value = JsonSerializer.Deserialize<TValue>(ref reader, options);
						break;
					default:
						reader.Skip();
						break;
				}
			}

			if (key is null) throw new JsonException("Missing Vector2I key.");
			if (value is not null) result[key.Value] = value;
		}

		throw new JsonException("Unexpected end of JSON.");
	}

	public override void Write(Utf8JsonWriter writer, Dictionary<Vector2I, TValue> value, JsonSerializerOptions options)
	{
		writer.WriteStartArray();
		foreach (var (key, entry) in value)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(KeyProp);
			JsonSerializer.Serialize(writer, key, options);
			writer.WritePropertyName(ValueProp);
			JsonSerializer.Serialize(writer, entry, options);
			writer.WriteEndObject();
		}
		writer.WriteEndArray();
	}
}
public sealed class Vector2IJsonConverter : JsonConverter<Vector2I>
{
	public override Vector2I Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("Expected start object for Vector2I.");

		int x = 0, y = 0;
		bool hasX = false, hasY = false;

		while (reader.Read())
		{
			if (reader.TokenType == JsonTokenType.EndObject)
			{
				if (!hasX || !hasY) throw new JsonException("Vector2I requires x and y.");
				return new Vector2I(x, y);
			}

			string name = reader.GetString()!;
			reader.Read();

			switch (name)
			{
				case "x":
					x = reader.GetInt32();
					hasX = true;
					break;
				case "y":
					y = reader.GetInt32();
					hasY = true;
					break;
				default:
					reader.Skip();
					break;
			}
		}
		throw new JsonException();
	}

	public override void Write(Utf8JsonWriter writer, Vector2I value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WriteNumber("x", value.X);
		writer.WriteNumber("y", value.Y);
		writer.WriteEndObject();
	}
}
