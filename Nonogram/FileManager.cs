using static System.Text.Json.JsonSerializer;
using Godot;

namespace RSG.Nonogram;

using static Display;

public static class FileManager
{
	public static class Paths
	{
		public const string
		Project = "res://",
		User = "user://",
		FileType = ".json",
		Save = "Saves";
	}

	public static string SavePath => (OS.HasFeature("editor") ? Paths.Project : OS.GetUserDataDir()) + "/" + Paths.Save;

	public static IList<SaveData> GetSaved()
	{
		IList<SaveData> puzzles = [];
		string globalPath = ProjectSettings.GlobalizePath(SavePath);
		try
		{
			if (!Directory.Exists(globalPath)) { return puzzles; }
			foreach (string path in Directory.EnumerateFiles(globalPath))
			{
				string json = File.ReadAllText(path);
				if (Deserialize(json, SaveJsonContext.Default.SaveData) is not SaveData data) continue;
				puzzles.Add(data);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr(exception);
			return puzzles;
		}
		return puzzles;
	}
	public static void Save(Data data)
	{
		string path = SavedPuzzlesPath(data.Name);
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
		string contents = data switch
		{
			SaveData save => Serialize(save, SaveJsonContext.Default.SaveData),
			PuzzleData puzzle => Serialize(puzzle, SaveJsonContext.Default.PuzzleData),
			_ => ""
		};
		File.WriteAllText(path, contents);
	}

	private static string SavedPuzzlesPath(string name)
	{
		string path = $"{SavePath}/{name}{Paths.FileType}";
		return ProjectSettings.GlobalizePath(path);
	}
}
