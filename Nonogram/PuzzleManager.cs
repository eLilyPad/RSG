namespace RSG.Nonogram;

using static PuzzleData;
using static Display;

public sealed partial class PuzzleManager
{
	public interface IHaveEvents
	{
		void Completed(SaveData puzzle) { }
		void SettingsChanged() { }
	}
	private sealed record class ManagedPuzzle() : CurrentPuzzle;

	public static CurrentPuzzle Current => field ??= new ManagedPuzzle();
	internal static PuzzleManager Instance => field ??= new();

	public static IEnumerable<(string Name, IEnumerable<SaveData> Data)> SelectorConfigs => [
		("Saved Puzzles", GetSavedPuzzles()),
		.. GetPuzzlePacks().Select(Pack.Convert)
	];
	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles() => FileManager.GetSaved();
	public static void Save(OneOf<PuzzleData, SaveData> puzzle) => puzzle.Switch(Instance.AddPuzzle, Instance.AddSave);

	private readonly List<Pack> PuzzlePacks = [Pack.Procedural()];
	private readonly Dictionary<string, bool> PuzzlesCompleted = [];
	private readonly Dictionary<string, string> CompletionDialogues = [];
	private readonly Dictionary<string, Data> Puzzles = new() { [Data.DefaultName] = new PuzzleData() };

	private PuzzleManager() { }

	public void AddSave(SaveData save)
	{
		save = save with { Name = save.Name + " - save" };
		FileManager.Save(save);
		Puzzles[save.Name] = save;
	}
	public void AddPuzzle(PuzzleData puzzle)
	{
		FileManager.Save(puzzle);
		Puzzles[puzzle.Name] = puzzle;
	}
}
