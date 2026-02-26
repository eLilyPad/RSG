using Godot;

namespace RSG.Nonogram;

using static Display;
using TRun = (int Index, int HintIndex, int FilledCount, int RunLength, int HintCount);
using HintLines = IImmutableList<IImmutableList<int>>;
using Puzzle = IEnumerable<KeyValuePair<Vector2I, Display.TileMode>>;
using PuzzleMasks = ReadOnlyCollection<ReadOnlyCollection<ulong>>;

public static class Solver
{
	private record Hints(HintLines Columns, HintLines Rows, PuzzleMasks Masks)
	{
		public int ScanningRow { get; set; } = 0;

		public ImmutableList<int> Totals => field ??= [.. Columns.Select(hints => hints.Sum())];
		public ulong[] ActiveScan => field ??= new ulong[Columns.Count];
		public int[] MaskIndex => field ??= new int[Masks.Count].Fill(-1);
		public bool ScannedRowComplete => MaskIndex[ScanningRow] >= Masks[ScanningRow].Count;
		public bool RowToScan => ScanningRow >= 0;

		public void ReplaceScan() => ActiveScan[ScanningRow] = Masks[ScanningRow][MaskIndex[ScanningRow]];
		public bool Matches()
		{
			List<int> rowGroups = [];
			int hintRun = 0;
			foreach ((int index, IReadOnlyList<int> hints) in Columns.Index())
			{
				for (int rowIndex = 0; rowIndex < Rows.Count; rowIndex++)
				{
					if (ActiveScan[rowIndex].IsFilled(index)) hintRun++;
					else if (hintRun > 0)
					{
						rowGroups.Add(hintRun);
						hintRun = 0;
					}
				}
				if (hintRun > 0) rowGroups.Add(hintRun);
				if (!rowGroups.SequenceEqual(hints)) return false;
			}
			return true;
		}
		public IEnumerable<(TRun value, IImmutableList<int> hints)> Get(Action<bool> columnPossible)
		{
			ulong[] currentRows = ActiveScan;
			foreach ((int index, IImmutableList<int> hints) in Columns.Index())
			{
				int count = hints.Count, hintIndex = 0, runLength = 0, filledCount = 0;
				for (int rowIndex = 0; rowIndex <= ScanningRow; rowIndex++)
				{
					ulong currentRowMask = currentRows[rowIndex];
					if (currentRowMask.IsFilled(index))
					{
						filledCount++;
						runLength++;
						if (hintIndex >= count || runLength > hints[hintIndex])
						{
							columnPossible(false);
							break;
						}
					}
					else if (runLength > 0)
					{
						hintIndex++;
						runLength = 0;
					}
				}
				if (filledCount > Totals[index])
				{
					columnPossible(false);
					break;
				}
				yield return ((index, hintIndex, filledCount, runLength, count), hints);
			}
		}
	}

	public static bool IsSolvable<TState>(this TState state, int size)
	where TState : Puzzle
	{
		Assert(state.IsSquare<TState, TileMode>(), $"State must be square");
		Hints hints = Create(state, size);
		bool columnStillPossible, matchExact;
		int remainingNeeded, remainingRows;
		while (hints.RowToScan)
		{
			columnStillPossible = true;
			if (hints.ScanningRow == hints.Masks.Count)
			{
				matchExact = true;
				if (!hints.Matches()) matchExact = false;
				if (matchExact) { return true; }
				hints.ScanningRow--;
				continue;
			}
			hints.MaskIndex[hints.ScanningRow]++;
			if (hints.ScannedRowComplete)
			{
				hints.MaskIndex[hints.ScanningRow] = -1;
				hints.ScanningRow--;
				continue;
			}
			hints.ReplaceScan();
			remainingRows = size - hints.ScanningRow - 1;

			foreach ((TRun run, IImmutableList<int> line) in hints.Get(columnPossible))
			{
				(int index, int hintIndex, int filledCount, int runLength, int hintCount) = run;
				bool runOver = runLength > 0;
				remainingNeeded = runOver ? line[hintIndex] - runLength : 0;
				int offset = runOver ? 1 : 0;
				for (int i = hintIndex + offset; i < hintCount; i++) remainingNeeded += line[i];

				if (remainingNeeded > remainingRows)
				{
					columnStillPossible = false;
					break;
				}
			}
			if (columnStillPossible) hints.ScanningRow++;
		}
		return false;

		void columnPossible(bool possible) => columnStillPossible = possible;
	}
	private static Hints Create(Puzzle state, int size)
	{
		HintLines columns = CalculateHint(Side.Column), rows = CalculateHint(Side.Row);
		return new Hints(columns, rows, GetMasks());

		HintLines CalculateHint(Side side)
		{
			IImmutableList<int>[] hints = new IImmutableList<int>[size];
			for (int i = 0; i < size; i++)
			{
				hints[i] = [.. state.AsLineHints(new(side, i))];
			}
			return [.. hints];
		}
		PuzzleMasks GetMasks()
		{
			int size = rows.Count;
			int nextPosition;
			ReadOnlyCollection<ulong>[] rowMasks = new ReadOnlyCollection<ulong>[size];

			foreach ((int index, IReadOnlyList<int> expectedRow) in rows.Index())
			{
				Stack<(int hintIndex, int position, ulong mask)> maskStack = new();
				List<ulong> masks = [];
				maskStack.Push((0, 0, 0UL));
				if (expectedRow.Count == 0)
				{
					masks.Add(0UL);
					rowMasks[index] = masks.AsReadOnly();
					continue;
				}
				while (maskStack.Count > 0)
				{
					(int hintIndex, int position, ulong mask) = maskStack.Pop();
					if (!expectedRow.TryGetValue(hintIndex, out int rowHintBlock))
					{
						if (!RowMaskMatchesPlayerState(mask, index)) continue;
						masks.Add(mask);
					}
					int remainingMin = expectedRow.Remaining(hintIndex);
					for (int start = size - rowHintBlock; start >= position; start--)
					{
						if ((nextPosition = start + rowHintBlock) + remainingMin > size) continue;
						ulong newMask = mask;
						for (int nextI = 0; nextI < rowHintBlock; nextI++) newMask |= 1UL << (start + nextI);
						if (hintIndex + 1 < expectedRow.Count) nextPosition++;
						maskStack.Push((hintIndex + 1, nextPosition, newMask));
					}
				}
				rowMasks[index] = masks.AsReadOnly();
			}
			return rowMasks.AsReadOnly();
		}
		bool RowMaskMatchesPlayerState(ulong mask, int rowIndex)
		{
			foreach ((Vector2I pos, TileMode mode) in state) // store state in Hints
			{
				if (pos.Y != rowIndex) continue;
				bool filled = ((mask >> pos.X) & 1UL) != 0;
				if (mode == TileMode.Filled && !filled) return false;
				if (mode == TileMode.Blocked && filled) return false;
			}
			return true;
		}
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFilled(this ulong mask, int index) => ((mask >> index) & 1UL) != 0;
}

