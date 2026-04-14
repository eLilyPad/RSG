namespace RSG;

using Nonogram;

using NonogramPuzzle = Nonogram.PuzzleData;

public static class NonogramNodePoolExtensions
{
	public static void LoadNonogramPuzzle<TPool, TPack, TDisplay>(
		this TPool pool,
		IEnumerable<NonogramPuzzle.Pack>? configs)
	where TPool : NodePool<string, TPack>, PuzzleSelector.Display.IConfigure<TDisplay>
	where TPack : PuzzleSelector.PackDisplay, new()
	where TDisplay : PuzzleSelector.Display, new()
	{
		configs ??= PuzzleManager.SelectorConfigs;
		foreach (NonogramPuzzle.Pack data in configs)
		{
			Assert(configs is not null, "Configs cannot be null.");

			PuzzleSelector.Display.IConfigure<TDisplay> configurator = pool;
			var displayPools = pool as IDictionary<string, IList<TDisplay>>;
			var id = data.Name;
			var displayParent = pool.GetOrCreate(id).Puzzles.Value;

			Assert(configs is not null, "Configs cannot be null.");
			Assert(displayPools is not null, "Pool must implement IDictionary<string, TDisplay[]>.");
			Assert(configurator is not null, "Pool must implement IConfigure<TDisplay>.");

			IList<TDisplay> displays = displayPools.GetOrCreate(id, create: _ => []);

			foreach ((int i, NonogramPuzzle puzzle) in data.Puzzles.Index())
			{
				if (displays.Count <= i) displays.Add(CreateLoadedDisplay(puzzle));
				else configurator.Configure(displays[i], puzzle);
			}

			TDisplay CreateLoadedDisplay(NonogramPuzzle puzzle)
			{
				TDisplay display = new() { Name = puzzle.Name };
				displayParent.AddChild(display);
				return configurator.Configure(display, puzzle);
			}
			return;
		}
	}
	public static void LoadNonogramPuzzleV2<TPool, TPack, TDisplay>(
		this TPool pool,
		IEnumerable<NonogramPuzzle.Pack>? configs)
	where TPool : NodePool<string, TPack>.PooledGrand<TDisplay>, PuzzleSelector.Display.IConfigure<TDisplay>
	where TPack : PuzzleSelector.PackDisplay, new()
	where TDisplay : PuzzleSelector.Display, new()
	{
		configs ??= PuzzleManager.SelectorConfigs;
		foreach (NonogramPuzzle.Pack data in configs)
		{

			PuzzleSelector.Display.IConfigure<TDisplay> configurator = pool;
			var id = data.Name;
			var displayParent = pool.GetOrCreate(id).Puzzles.Value;

			Assert(configs is not null, "Configs cannot be null.");
			Assert(configurator is not null, "Pool must implement IConfigure<TDisplay>.");

			IList<TDisplay> displays = pool.GetGrandChildren(id);

			foreach ((int i, NonogramPuzzle puzzle) in data.Puzzles.Index())
			{
				if (displays.Count <= i) displays.Add(CreateLoadedDisplay(puzzle));
				else configurator.Configure(displays[i], puzzle);
			}

			TDisplay CreateLoadedDisplay(NonogramPuzzle puzzle)
			{
				TDisplay display = new() { Name = puzzle.Name };
				displayParent.AddChild(display);
				return configurator.Configure(display, puzzle);
			}
			return;
		}
	}
}