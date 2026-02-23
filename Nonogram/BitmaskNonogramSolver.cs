namespace RSG.Nonogram;

public static class Solver
{
	public static bool IsSolvable<T>(this T state, IPuzzleHints config)
	where T : IEnumerable<KeyValuePair<Godot.Vector2I, Display.TileMode>>
	{
		Assert(state.IsSquare<T, Display.TileMode>(), $"State must be square");
		int size = config.PuzzleSize;
		IReadOnlyList<IReadOnlyList<int>> expectedColumnsHints = config.ColumnHints;
		IReadOnlyList<IReadOnlyList<int>> expectedRowsHints = config.RowHints;
		List<ulong>[] rowMasks = new List<ulong>[size];
		ulong[] currentRows = new ulong[size];
		int[] columnTotals = new int[size];
		int[] maskIndex = new int[rowMasks.Length];
		Stack<(int hintIndex, int position, ulong mask)> maskStack = new();
		List<ulong> masks = [];
		List<int> rowGroups = [];
		int row = 0;
		int columnTotal;
		bool columnStillPossible;
		bool matchExact;
		int columnHintRun;
		ulong rowMask;
		int remainingRows = size - 1;
		int remainingMin;
		int hintCount;
		int hintIndex;
		int runLength;
		int filledCount;
		int remainingNeeded;
		int rowHintBlock;
		int nextPosition;
		ulong newMask;

		Array.Fill(maskIndex, -1);

		for (int columnIndex = 0; columnIndex < size; columnIndex++)
		{
			columnTotal = 0;
			IReadOnlyList<int> expectedHints = expectedColumnsHints[columnIndex];
			for (int index = 0; index < expectedHints.Count; index++)
			{
				columnTotal += expectedHints[index];
			}
			columnTotals[columnIndex] = columnTotal;
		}
		foreach ((int i, IReadOnlyList<int> expectedRow) in expectedRowsHints.Index())
		{
			if (expectedRow.Count == 0)
			{
				masks.Add(0UL);
				rowMasks[i] = masks;
				continue;
			}
			maskStack.Clear();
			maskStack.Push((0, 0, 0UL));

			while (maskStack.Count > 0)
			{
				(hintIndex, int position, ulong mask) = maskStack.Pop();
				if (hintIndex == expectedRow.Count)
				{
					masks.Add(mask);
					continue;
				}
				rowHintBlock = expectedRow[hintIndex];
				remainingMin = 0;
				for (int nextIndex = hintIndex + 1; nextIndex < expectedRow.Count; nextIndex++)
				{
					remainingMin += expectedRow[nextIndex] + 1;
				}
				for (int start = size - rowHintBlock; start >= position; start--)
				{
					nextPosition = start + rowHintBlock;
					if (nextPosition + remainingMin > size)
					{
						continue;
					}
					newMask = mask;
					for (int nextIndex = 0; nextIndex < rowHintBlock; nextIndex++)
					{
						newMask |= 1UL << (start + nextIndex);
					}
					if (hintIndex + 1 < expectedRow.Count)
					{
						nextPosition++;
					}
					maskStack.Push((hintIndex + 1, nextPosition, newMask));
				}
			}

			rowMasks[i] = masks;
			masks.Clear();
		}
		while (row >= 0)
		{
			if (row == rowMasks.Length)
			{
				matchExact = true;
				rowGroups.Clear();
				foreach ((int index, IReadOnlyList<int> hints) in expectedColumnsHints.Index())
				{
					columnHintRun = 0;
					for (int rowIndex = 0; rowIndex < expectedRowsHints.Count; rowIndex++)
					{
						rowMask = currentRows[rowIndex];
						if (((rowMask >> index) & 1UL) != 0)
						{
							columnHintRun++;
						}
						else if (columnHintRun > 0)
						{
							rowGroups.Add(columnHintRun);
							columnHintRun = 0;
						}
					}
					if (columnHintRun > 0)
					{
						rowGroups.Add(columnHintRun);
					}
					if (!rowGroups.SequenceEqual(hints))
					{
						matchExact = false;
					}
					rowGroups.Clear();
				}
				if (matchExact)
				{
					return true;
				}
				row--;
				continue;
			}
			maskIndex[row]++;
			if (maskIndex[row] >= rowMasks[row].Count)
			{
				maskIndex[row] = -1;
				row--;
				continue;
			}
			currentRows[row] = rowMasks[row][maskIndex[row]];

			columnStillPossible = true;
			remainingRows -= row;

			for (int column = 0; column < size; column++)
			{
				IReadOnlyList<int> hints = expectedColumnsHints[column];
				hintCount = hints.Count;
				hintIndex = 0;
				runLength = 0;
				filledCount = 0;
				for (int r = 0; r <= row; r++)
				{
					if (((currentRows[r] >> column) & 1UL) != 0)
					{
						filledCount++;
						runLength++;
						if (hintIndex >= hintCount || runLength > hints[hintIndex])
						{
							columnStillPossible = false;
							break;
						}
					}
					else if (runLength > 0)
					{
						hintIndex++;
						runLength = 0;
					}
				}
				if (filledCount > columnTotals[column])
				{
					columnStillPossible = false;
					break;
				}
				remainingNeeded = 0;
				if (runLength > 0)
				{
					remainingNeeded += hints[hintIndex] - runLength;
					for (int i = hintIndex + 1; i < hintCount; i++)
					{
						remainingNeeded += hints[i];
					}
				}
				else
				{
					for (int i = hintIndex; i < hintCount; i++)
					{
						remainingNeeded += hints[i];
					}
				}
				if (remainingNeeded > remainingRows)
				{
					columnStillPossible = false;
					break;
				}
			}
			if (columnStillPossible)
			{
				row++;
			}
		}
		return false;
	}
}

