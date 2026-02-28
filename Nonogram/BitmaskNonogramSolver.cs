using Godot;

namespace RSG.Nonogram;

using static Display;
using TRun = (int Index, int HintIndex, int FilledCount, int RunLength, int HintCount);
using HintLines = IImmutableList<IImmutableList<int>>;
using Puzzle = IEnumerable<KeyValuePair<Vector2I, Display.TileMode>>;
using PuzzleMasks = ulong[][];

public static class Solver
{
	private record Hints(HintLines Columns, HintLines Rows, PuzzleMasks Masks)
	{
		public int ScanningRow { get; set; } = 0;

		public ImmutableList<int> Totals => field ??= [.. Columns.Select(hints => hints.Sum())];
		public ulong[] ActiveScan => field ??= new ulong[Columns.Count];
		public int[] MaskIndex => field ??= new int[Masks.Length].Fill(-1);
		public bool ScannedRowComplete => MaskIndex[ScanningRow] >= Masks[ScanningRow].Length;
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
	public static bool IsSolvable(this SaveData save, int size)
	{
		Puzzle state = save.Expected.States;
		Assert(state.IsSquare<Puzzle, TileMode>(), $"State must be square");
		Hints hints = Create(save);
		bool columnStillPossible, matchExact;
		int remainingNeeded, remainingRows;
		while (hints.RowToScan)
		{
			columnStillPossible = true;
			PuzzleMasks masks = hints.Masks;
			if (hints.ScanningRow == masks.Length)
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
	private static Hints Create(Data data)
	{
		HintLines columns = CalculateHint(Side.Column), rows = CalculateHint(Side.Row);
		ulong[][] masks = Data.DataMask(Side.Row, data);
		return new Hints(columns, rows, masks);
		HintLines CalculateHint(Side side) => [.. Data.FilledLines(side, data)
			.Select<int[], IImmutableList<int>>(selector: a => [.. a])
		];
	}
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static bool IsFilled(this ulong mask, int index) => ((mask >> index) & 1UL) != 0;
}

