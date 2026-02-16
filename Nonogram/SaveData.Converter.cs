using static System.Text.Json.JsonSerializer;
using System.Text.Json.Serialization;
using System.Text.Json;
using Godot;

namespace RSG.Nonogram;

using Mode = Display.TileMode;
public sealed partial record SaveData
{
	public sealed class Converter : JsonConverter<SaveData>
	{
		public const string ExpectedProp = "Expected";

		public override SaveData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				GD.PrintErr("SaveData root is not an object");
				return null;
			}
			PuzzleData? expected = null;
			Dictionary<Vector2I, Mode>? tiles = null;
			TimeSpan timeTaken = TimeSpan.Zero;

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject) break;
				if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException();

				string prop = reader.GetString()!;
				reader.Read();

				switch (prop)
				{
					case ExpectedProp:
						expected = Deserialize<PuzzleData>(ref reader, options);
						break;
					case PropertyNames.Tiles:
						tiles = Deserialize<Dictionary<Vector2I, Mode>>(ref reader, options);
						break;
					case PropertyNames.TimeTaken:
						timeTaken = ReadTimeSpan(ref reader, options);
						break;
					default:
						reader.Skip();
						break;
				}
			}

			if (expected is null)
			{
				GD.PrintErr($"Missing property in JSON: {ExpectedProp}");
				return null;
			}
			if (tiles is null) return null;

			return new SaveData
			{
				Expected = expected,
				Tiles = tiles,
				TimeTaken = timeTaken
			};
		}
		public override void Write(Utf8JsonWriter writer, SaveData value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WritePropertyName(ExpectedProp);
			Serialize(writer, value.Expected, options);
			writer.WritePropertyName(PropertyNames.TimeTaken);
			Serialize(writer, value.TimeTaken, options);
			writer.WritePropertyName(PropertyNames.Tiles);
			Serialize(writer, value.Tiles, options);
			writer.WriteEndObject();
		}
		private static TimeSpan ReadTimeSpan(ref Utf8JsonReader reader, JsonSerializerOptions options)
		{
			try
			{
				return reader.TokenType switch
				{
					JsonTokenType.String =>
						Deserialize<TimeSpan>(ref reader, options),

					JsonTokenType.Number =>
						TimeSpan.FromSeconds(reader.GetDouble()),

					_ => TimeSpan.Zero
				};
			}
			catch (Exception e)
			{
				GD.PrintErr($"Invalid TimeTaken value: {e.Message}");
				return TimeSpan.Zero;
			}
		}
	}
}
