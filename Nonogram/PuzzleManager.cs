using static System.Text.Json.JsonSerializer;
using Godot;

namespace RSG.Nonogram;

using static PuzzleData;
using static NonogramContainer;

using LoadResult = OneOf<Display.Data, PuzzleData.Code.ConversionError, NotFound>;

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
				if (Deserialize<SaveData>(json, options: Converter.Options) is not SaveData data) continue;
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
	public static void Save(Display.Data data)
	{
		string path = SavedPuzzlesPath(data.Name);
		Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
		string contents = data switch
		{
			SaveData save => Serialize(save, Converter.Options),
			PuzzleData puzzle => Serialize(puzzle, Converter.Options),
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

public sealed class PuzzleManager
{
	public sealed record class CurrentPuzzle
	{
		public Display.Data Puzzle
		{
			get; set
			{
				if (value is null) { return; }
				field = Instance.Puzzles[value.Name] = value;
				CheckCompletion();
				Display.Load(value);
			}
		} = new SaveData();
		public Display Display { private get; set => (field = value).Load(Puzzle); } = Display.Default;

		/// <summary>
		/// Updates the current puzzle from the display.
		///  - saves the changes to puzzle.
		///  - Writes to game display
		/// Checks if the display has StatusBar then  
		/// </summary>
		public void SaveProgress()
		{
			SaveData data = new(Puzzle, Display);
			Save(data);
			Puzzle = data;
		}
		public bool CheckCompletion()
		{
			switch (Display)
			{
				case GameDisplay game when Puzzle is SaveData save:
					if (Instance.PuzzlesCompleted[Current.Puzzle.Name] = save.IsComplete)
					{
						game.CompletionScreen.Show();
						game.Status.CompletionLabel.Text = StatusBar.PuzzleComplete;
						if (save.Expected.DialogueName is string dialogueName)
						{
							GD.Print("starting dialogue " + dialogueName);
							Dialogues.Start(dialogueName, true);
						}
						return true;
					}

					game.Status.CompletionLabel.Text = StatusBar.PuzzleIncomplete;
					break;
			}
			return false;
		}
	}

	public static CurrentPuzzle Current => field ??= new();
	private static PuzzleManager Instance => field ??= new();

	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles() => FileManager.GetSaved();
	public static LoadResult Load(OneOf<string, Display.Data> value)
	{
		return value.Match(LoadCode, LoadData);
		static LoadResult LoadData(Display.Data data) => Instance.Puzzles[data.Name] = data switch
		{
			SaveData save => save.Expected,
			_ => data
		};
		static LoadResult LoadCode(string code) => Code.Encode(code).Match<LoadResult>(
			error => error,
			code =>
			{
				PuzzleData data = code.Decode();
				return Instance.Puzzles[data.Name] = data;
			}
		);
	}
	public static void Save(OneOf<PuzzleData, SaveData> puzzle)
	{
		puzzle.Switch(Puzzle, Savable);
		static void Savable(SaveData save)
		{
			FileManager.Save(save);
			Instance.Puzzles[save.Name] = save.Expected;
		}
		static void Puzzle(PuzzleData data)
		{
			FileManager.Save(data);
			Instance.Puzzles[data.Name] = data;
		}
	}

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
	public Dictionary<string, bool> PuzzlesCompleted { private get; init; } = [];
	public Dictionary<string, Display.Data> Puzzles { private get; init; } = new()
	{
		[Display.Data.DefaultName] = new PuzzleData()
	};

	private PuzzleManager() { }
}
