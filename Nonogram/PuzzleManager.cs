namespace RSG.Nonogram;

using static PuzzleData;

using static Display;

public sealed partial class PuzzleManager
{
	public interface IChangeWithSettings { void SettingsChanged(); }
	public interface INotifyCompletion { void Completed(SaveData puzzle); }
	public interface IHaveEvents : IChangeWithSettings, INotifyCompletion;

	public static CurrentPuzzle Current => field ??= new();
	internal static PuzzleManager Instance => field ??= new();

	public static IEnumerable<(string Name, IEnumerable<SaveData> Data)> SelectorConfigs => [
		("Saved Puzzles", GetSavedPuzzles()),
		.. GetPuzzlePacks().Select(Pack.Convert)
	];
	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles() => FileManager.GetSaved();
	public static void Save(OneOf<PuzzleData, SaveData> puzzle)
	{
		puzzle.Switch(Puzzle, Savable);
		static void Savable(SaveData save)
		{
			FileManager.Save(save);
			Instance.Puzzles[save.Name] = save;
		}
		static void Puzzle(PuzzleData data)
		{
			FileManager.Save(data);
			Instance.Puzzles[data.Name] = data;
		}
	}

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
	public Dictionary<string, Data> Puzzles { internal get; init; } = new() { [Data.DefaultName] = new PuzzleData() };

	private PuzzleManager() { }
}
