using System.Text.Json;
using System.Text.Json.Serialization;

namespace RSG.Nonogram;

[JsonSourceGenerationOptions(
	WriteIndented = true,
	PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
	Converters = [
		typeof(SaveData.Converter),
		typeof(PuzzleData.Converter),
		typeof(Vector2IDictionaryConverter<Display.TileMode>),
		typeof(Vector2IJsonConverter)
	]
)]
[JsonSerializable(typeof(SaveData))]
[JsonSerializable(typeof(PuzzleData))]
[JsonSerializable(typeof(TimeSpan))]
public partial class SaveJsonContext : JsonSerializerContext;