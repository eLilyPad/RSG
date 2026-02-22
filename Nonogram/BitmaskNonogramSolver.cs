namespace RSG.Nonogram;

public static class Solver
{
	public static bool HasSolution(List<int>[] rowHints, List<int>[] colHints)
	{
		return new BitmaskNonogramSolver(rowHints, colHints).HasSolution();
	}
	public static bool HasUniqueSolution(List<int>[] rowHints, List<int>[] colHints)
	{
		return new BitmaskNonogramSolver(rowHints, colHints).HasUniqueSolution();
	}

	private sealed class BitmaskNonogramSolver
	{
		private readonly int _width;
		private readonly int _height;

		private readonly List<int>[] _rowHints;
		private readonly List<int>[] _colHints;

		private readonly List<ulong>[] _rowMasks;
		private readonly ulong[] _currentRows;

		private int _solutionCount;

		public BitmaskNonogramSolver(List<int>[] rowHints, List<int>[] colHints)
		{
			Assert(colHints.Length > 64, "Bitmask solver supports max width of 64.");
			_rowHints = rowHints;
			_colHints = colHints;
			_height = rowHints.Length;
			_width = colHints.Length;
			_currentRows = new ulong[_height];
			_rowMasks = new List<ulong>[_height];

			for (int r = 0; r < _height; r++)
			{
				List<ulong> results = [];
				Generate(_width, _rowHints[r], 0, 0, 0UL, results);
				_rowMasks[r] = results;
			}
		}
		public bool HasSolution()
		{
			Solve(_solutionCount = 0, stopAtTwo: false);
			return _solutionCount > 0;
		}
		public bool HasUniqueSolution()
		{
			Solve(_solutionCount = 0, stopAtTwo: true);
			return _solutionCount == 1;
		}

		private bool Solve(int row, bool stopAtTwo)
		{
			if (row == _height)
			{
				if (ColumnsMatchExact())
				{
					_solutionCount++;
					return stopAtTwo && _solutionCount >= 2;
				}
				return false;
			}

			foreach (var mask in _rowMasks[row])
			{
				_currentRows[row] = mask;

				if (ColumnsStillPossible(row))
				{
					if (Solve(row + 1, stopAtTwo))
						return true;
				}
			}

			return false;

			bool ColumnsMatchExact()
			{
				for (int c = 0; c < _width; c++)
				{
					List<int> groups = [];
					int run = 0;

					for (int r = 0; r < _height; r++)
					{
						bool filled = ((_currentRows[r] >> c) & 1UL) != 0;

						if (filled) run++;
						else if (run > 0)
						{
							groups.Add(run);
							run = 0;
						}
					}

					if (run > 0) groups.Add(run);
					if (!groups.SequenceEqual(_colHints[c])) return false;
				}

				return true;
			}
			bool ColumnsStillPossible(int filledRows)
			{
				for (int c = 0; c < _width; c++)
				{
					int hintIndex = 0;
					int runLength = 0;

					for (int r = 0; r <= filledRows; r++)
					{
						bool filled = ((_currentRows[r] >> c) & 1UL) != 0;
						if (!filled && runLength > 0)
						{
							hintIndex++;
							runLength = 0;
						}
						if (!filled) continue;

						List<int> hints = _colHints[c];
						runLength++;
						bool isNotPossible = hintIndex >= hints.Count || runLength > hints[hintIndex];
						if (isNotPossible) return false;
					}
				}

				return true;
			}
		}

		private static void Generate(int width, List<int> hints, int hintIndex,
			int position, ulong mask, List<ulong> results)
		{
			if (hintIndex == hints.Count)
			{
				results.Add(mask);
				return;
			}

			int block = hints[hintIndex];

			for (int start = position; start + block <= width; start++)
			{
				ulong newMask = mask;

				for (int i = 0; i < block; i++)
					newMask |= 1UL << (start + i);

				int nextPos = start + block + 1;

				Generate(width, hints, hintIndex + 1, nextPos, newMask, results);
			}
		}
	}
}

