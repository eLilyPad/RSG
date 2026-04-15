namespace RSG.Nonogram;

using static PuzzleData;

using static Display;

public sealed partial class PuzzleManager
{
	public interface IChangeWithSettings { void SettingsChanged(); }
	public interface INotifyCompletion { void Completed(SaveData puzzle); }
	public interface IHaveEvents : IChangeWithSettings, INotifyCompletion;

	internal static PuzzleManager Instance => field ??= new();

	public const string SavedPackName = "Saved Puzzles";
	public static IReadOnlyList<Pack> SelectorConfigs => [
		new([.. FileManager.GetSaved()], SavedPackName),
		Pack.Procedural
	];
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
	public Dictionary<string, Data> Puzzles { internal get; init; } = new() { [Data.DefaultName] = new PuzzleData() };

	private PuzzleManager() { }
}
