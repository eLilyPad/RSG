namespace RSG.Nonogram;

using static PuzzleData;

public interface IRefresh<TNode, TConfig>
{
	void Refresh(TNode node, TConfig config);
}
public sealed partial class PuzzleManager
{

	internal static PuzzleManager Instance => field ??= new();

	public static IEnumerable<PuzzleSelector.PackDisplay.Config> SelectorConfigs => [
		new PuzzleSelector.PackDisplay.Config("Saved Puzzles", GetSavedPuzzles()),
		.. GetPuzzlePacks().Select(Pack.Convert)
	];
	public static IReadOnlyList<Pack> GetPuzzlePacks() => [.. Instance.PuzzlePacks];
	public static IList<SaveData> GetSavedPuzzles() => FileManager.GetSaved();
	public static SaveData Save(SaveData puzzle)
	{
		puzzle = puzzle with { Name = puzzle.Name + " save" };
		FileManager.Save(puzzle);
		Instance.Puzzles[puzzle.Name] = puzzle;
		return puzzle;
	}

	public List<Pack> PuzzlePacks { get; } = [Pack.Procedural()];
	public Dictionary<string, bool> PuzzlesCompleted { private get; init; } = [];
	public Dictionary<string, string> CompletionDialogues { private get; init; } = [];
	public Dictionary<string, Display.Data> Puzzles { private get; init; } = new() { [Display.Data.DefaultName] = new PuzzleData() };

	private PuzzleManager() { }
}
