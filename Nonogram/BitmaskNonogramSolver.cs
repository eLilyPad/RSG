namespace RSG.Nonogram;

public static class Solver
{
	private static void GenerateRowNonRecursive(List<ulong> results, IReadOnlyList<int> hints, int size)
	{
		if (hints.Count == 0)
		{
			results.Add(0UL);
			return;
		}

		Stack<(int hintIndex, int position, ulong mask)> stack = new();
		stack.Push((0, 0, 0UL));

		while (stack.Count > 0)
		{
			var (hintIndex, position, mask) = stack.Pop();

			if (hintIndex == hints.Count)
			{
				results.Add(mask);
				continue;
			}

			int block = hints[hintIndex];
			int remainingMin = 0;
			for (int i = hintIndex + 1; i < hints.Count; i++) remainingMin += hints[i] + 1;

			for (int start = size - block; start >= position; start--)
			{
				int nextPosition = start + block;
				if (nextPosition + remainingMin > size) continue;
				ulong newMask = mask;
				for (int i = 0; i < block; i++) newMask |= 1UL << (start + i);
				if (hintIndex + 1 < hints.Count) nextPosition++;
				stack.Push((hintIndex + 1, nextPosition, newMask));
			}
		}
	}

	public static int Solutions<T>(this T state, IPuzzleHints config)
	where T : IEnumerable<KeyValuePair<Godot.Vector2I, Display.TileMode>>
	{
		Assert(state.IsSquare<T, Display.TileMode>(), $"State must be square");
		int solutionCount = 0;
		int size = config.PuzzleSize;
		List<ulong>[] rowMasks = new List<ulong>[size];
		ulong[] currentRows = new ulong[size];
		int[] columnTotals = new int[size];

		for (int c = 0; c < size; c++)
		{
			int total = 0;
			IReadOnlyList<int> hints = config.ColumnHints[c];
			for (int i = 0; i < hints.Count; i++) total += hints[i];
			columnTotals[c] = total;
		}

		foreach ((int i, IReadOnlyList<int> hints) in config.RowHints.Index())
		{
			List<ulong> results = [];
			GenerateRowNonRecursive(results, hints, size);
			rowMasks[i] = results;
		}

		Solve(row: solutionCount);
		return solutionCount;

		bool Solve(int row)
		{
			if (row == rowMasks.Length)
			{
				solutionCount++;
				return ColumnsMatchExact() && solutionCount >= 2;
			}
			foreach (ulong mask in rowMasks[row])
			{
				currentRows[row] = mask;

				if (ColumnsStillPossible(filledRows: row))
				{
					if (Solve(row: row + 1)) { return true; }
				}
			}
			return false;
		}
		void GenerateRowNonRecursive(List<ulong> results, IReadOnlyList<int> hints, int size)
		{
			if (hints.Count == 0)
			{
				results.Add(0UL);
				return;
			}

			Stack<(int hintIndex, int position, ulong mask)> stack = new();
			stack.Push((0, 0, 0UL));

			while (stack.Count > 0)
			{
				var (hintIndex, position, mask) = stack.Pop();

				if (hintIndex == hints.Count)
				{
					results.Add(mask);
					continue;
				}

				int block = hints[hintIndex];
				int remainingMin = 0;
				for (int i = hintIndex + 1; i < hints.Count; i++) remainingMin += hints[i] + 1;

				for (int start = size - block; start >= position; start--)
				{
					int nextPosition = start + block;
					if (nextPosition + remainingMin > size) continue;
					ulong newMask = mask;
					for (int i = 0; i < block; i++) newMask |= 1UL << (start + i);
					if (hintIndex + 1 < hints.Count) nextPosition++;
					stack.Push((hintIndex + 1, nextPosition, newMask));
				}
			}
		}
		bool ColumnsMatchExact()
		{
			List<int> groups = [];
			foreach ((int i, IReadOnlyList<int> hints) in config.ColumnHints.Index())
			{
				int run = 0;
				for (int row = 0; row < config.RowHints.Count; row++)
				{
					ulong rows = currentRows[row];
					bool filled = ((rows >> i) & 1UL) != 0;
					if (filled) run++;
					else if (run > 0)
					{
						groups.Add(run);
						run = 0;
					}
				}
				if (run > 0) groups.Add(run);
				if (!groups.SequenceEqual(hints)) return false;
				groups.Clear();
			}
			return true;
		}
		bool ColumnsStillPossible(int filledRows)
		{
			int size = config.PuzzleSize;
			int remainingRows = size - filledRows - 1;

			for (int column = 0; column < size; column++)
			{
				var hints = config.ColumnHints[column];
				int hintCount = hints.Count;

				int hintIndex = 0;
				int runLength = 0;
				int filledCount = 0;

				// Walk filled rows only
				for (int row = 0; row <= filledRows; row++)
				{
					bool filled = ((currentRows[row] >> column) & 1UL) != 0;

					if (filled)
					{
						filledCount++;
						runLength++;

						// Run too long?
						if (hintIndex >= hintCount || runLength > hints[hintIndex])
							return false;
					}
					else if (runLength > 0)
					{
						hintIndex++;
						runLength = 0;
					}
				}

				// Too many filled cells overall?
				if (filledCount > columnTotals[column])
					return false;

				// Remaining cells required to satisfy hints
				int remainingNeeded = 0;

				if (runLength > 0)
				{
					// Still inside a run
					remainingNeeded += hints[hintIndex] - runLength;

					for (int i = hintIndex + 1; i < hintCount; i++)
						remainingNeeded += hints[i];
				}
				else
				{
					for (int i = hintIndex; i < hintCount; i++)
						remainingNeeded += hints[i];
				}

				if (remainingNeeded > remainingRows)
					return false;
			}

			return true;
		}
	}
}

