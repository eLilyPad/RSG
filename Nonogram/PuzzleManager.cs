using static System.Text.Json.JsonSerializer;
using Godot;

namespace RSG.Nonogram;

using static PuzzleData;

using LoadResult = OneOf<Display.Data, PuzzleData.Code.ConversionError, NotFound>;

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
				if (Display is null)
				{
					return;
				}
				Display.Load(value);
			}
		} = new SaveData();
		public Display? Display
		{
			private get; set
			{
				Assert(value is not null, "Display value is null, unable to change current display");
				(field = value).Load(Puzzle);
			}
		} = null;

		public void Save()
		{
			Assert(Display is not null, "Display is null unable to save puzzle");
			SaveData data = new(Puzzle, Display);
			PuzzleManager.Save(data);
			Puzzle = data;
		}
		public bool IsComplete()
		{
			if (Display is null || !Current.Puzzle.Matches(Display)) return false;
			Instance.PuzzlesCompleted[Current.Puzzle.Name] = true;
			return true;
		}
		private Display? ChangeDisplay(Display? value)
		{
			if (value is null) return null;
			value.Load(Puzzle);
			return value;
		}
	}
	private const string RootPath = "res://", FileType = ".json", SavePath = "Saves";

	public static CurrentPuzzle Current => field ??= new();
	private static PuzzleManager Instance => field ??= new();

	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles()
	{
		IList<SaveData> puzzles = [];
		string savePath = ProjectSettings.GlobalizePath(RootPath + "/" + SavePath);
		try
		{
			foreach (string path in Directory.EnumerateFiles(savePath))
			{
				string json = File.ReadAllText(path);
				if (Deserialize<SaveData>(json, options: Converter.Options) is not SaveData data)
				{
					continue;
				}
				puzzles.Add(data);
			}
		}
		catch (Exception exception)
		{
			GD.PrintErr(exception);
			return [];
		}
		return puzzles;
	}
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
	public static void Save(OneOf<PuzzleData, Display.Data.Empty, SaveData> puzzle)
	{
		puzzle.Switch(Puzzle, Empty, Savable);
		static void Empty(Display.Data.Empty empty) => Puzzle(new(empty));
		static void Savable(SaveData save)
		{
			string path = SavedPuzzlesPath(save.Expected.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			string contents = Serialize(save, Converter.Options);
			File.WriteAllText(path, contents);
			Instance.Puzzles[save.Expected.Name] = save.Expected;
		}
		static void Puzzle(PuzzleData data)
		{
			string path = SavedPuzzlesPath(data.Name);
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? "");
			File.WriteAllText(path, contents: Serialize(data, Converter.Options));
			Instance.Puzzles[data.Name] = data;
		}
	}

	private static string SavedPuzzlesPath(string name)
	{
		string path = $"{RootPath}/{SavePath}/{name}{FileType}";
		return ProjectSettings.GlobalizePath(path);
	}

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
	public Dictionary<string, bool> PuzzlesCompleted { private get; init; } = [];
	public Dictionary<string, Display.Data> Puzzles { private get; init; } = new()
	{
		[Display.Data.DefaultName] = new PuzzleData()
	};

	private PuzzleManager() { }
}
