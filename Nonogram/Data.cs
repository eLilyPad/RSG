using Godot;

namespace RSG.Nonogram;

public abstract partial class Display
{
	public abstract class Data
	{
		public static class PropertyNames
		{
			public const string
			Tiles = "Tiles",
			Name = "Name",
			Position = "Position",
			Value = "Value",
			DialogueName = "CompletionDialogueName",
			TimeTaken = "TimeTaken";
		}
		public static Dictionary<Vector2I, TileMode> CreateTiles(int size) => (Vector2I.One * size)
			.GridRange().ToDictionary(elementSelector: _ => TileMode.Clear);

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public virtual string Name { get; set; } = DefaultName;
		public abstract Dictionary<Vector2I, TileMode> Tiles { protected get; init; }
		public Action<Vector2I>? Modified { get; set; }

		public bool IsSquare => _isTilesSquare ??= Tiles.IsSquare<Dictionary<Vector2I, TileMode>, TileMode>();
		public IImmutableDictionary<Vector2I, TileMode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		protected ulong[][] RowMasks => field ??= DataMask(Side.Row, this);
		protected ulong[][] ColumnMasks => field ??= DataMask(Side.Column, this);
		protected int[][] Rows => field ??= FilledLines(Side.Row, this);
		protected int[][] Columns => field ??= FilledLines(Side.Column, this);
		private bool? _isTilesSquare;
		public Data(int size = DefaultSize) { Tiles = CreateTiles(size); }
		public Data(string name, Func<Vector2I, bool> selector, int size)
		{
			Name = name;
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(
				elementSelector: position => selector(position) ? TileMode.Filled : TileMode.Clear
			);
		}
		public IEnumerable<int> Line() { for (int i = 0; i < Size; i++) yield return i; }
		//public IEnumerable<(int index, int length)> LineBlocks(){ }

		public IEnumerable<(Vector2I Position, TileMode Mode)> InLine(HintPosition position)
			=> InLine(position: position.Origin, side: position);
		public IEnumerable<(Vector2I Position, TileMode Mode)> InLine(int index, Side side)
			=> InLine(position: side.Origin() * index, side);
		public IEnumerable<(Vector2I Position, TileMode Mode)> InLine(Vector2I position, Side side)
		{
			const int step = 1;
			side.Shift(ref position, amount: -side.OrderFrom(position));
			for (int i = 0; i < Size; i += step)
			{
				side.Shift(ref position, step);
				if (!Tiles.TryGetValue(position, out TileMode mode)) continue;
				yield return (position, mode);
			}
		}
		public IEnumerable<(Vector2I Position, TileMode Mode)> InLines(Vector2I position)
		{
			foreach ((Vector2I linePosition, TileMode lineMode) in InLine(position, Side.Row))
			{
				yield return (linePosition, lineMode);
			}
			foreach ((Vector2I linePosition, TileMode lineMode) in InLine(position, Side.Column))
			{
				yield return (linePosition, lineMode);
			}
		}
		internal void ChangeState(Vector2I position, TileMode mode)
		{
			Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
			Tiles[position] = mode;
			Modified?.Invoke(position);
		}
		private static int[][] FilledLines(Side side, Data data)
		{
			int[][] values = new int[data.Size][];
			for (int i = 0; i < data.Size; i++)
			{
				HintPosition position = new(side, i);
				values[i] = data
					.InLine(position)
					.Select(pair => pair.Mode is TileMode.Filled ? 1 : 0)
					.Condense()
					is int[] { Length: 0 } a
					? a
					: [0];
			}
			return values;
		}
		private static ulong[][] DataMask(Side side, Data data)
		{
			const ulong nothing = 0UL;
			int size = data.Size;
			ulong[][] lines = new ulong[size][];
			int[][] hintLines = side switch { Side.Row => data.Rows, _ => data.Columns };
			for (int i = 0; i < data.Size; i++)
			{
				int[] hints = hintLines[i];
				Stack<(int hintIndex, int position, ulong mask)> maskStack = new();
				List<ulong> possibilities = [];
				maskStack.Push(item: (0, 0, nothing));
				bool noHints = hints.Length is 0;
				if (noHints)
				{
					possibilities.Add(nothing);
					lines[i] = [.. possibilities];
					continue;
				}
				while (maskStack.Count > 0)
				{
					(int hintIndex, int position, ulong mask) = maskStack.Pop();
					if (!hints.TryGetValue(hintIndex, out int rowHintBlock))
					{
						if (!LineMaskMatchesPlayerState(mask, i)) continue;
						possibilities.Add(mask);
					}
					int remainingMin = hints.Remaining(hintIndex);
					for (int start = size - rowHintBlock; start >= position; start--)
					{
						int nextPosition;
						if ((nextPosition = start + rowHintBlock) + remainingMin > size) continue;
						ulong newMask = mask;
						for (int nextI = 0; nextI < rowHintBlock; nextI++) newMask |= 1UL << (start + nextI);
						if (hintIndex + 1 < hints.Length) nextPosition++;
						maskStack.Push((hintIndex + 1, nextPosition, newMask));
					}
				}
				lines[i] = [.. possibilities];
			}
			return lines;

			bool LineMaskMatchesPlayerState(ulong mask, int index)
			{
				foreach ((Vector2I pos, TileMode mode) in data.InLine(index, side))
				{
					bool filled = ((mask >> side.OrderFrom(pos)) & 1UL) != 0;
					if (mode == TileMode.Filled && !filled) return false;
					if (mode == TileMode.Blocked && filled) return false;
				}
				return true;
			}
		}
	}
	public enum Side : byte { Row = 0, Column = 1 }
}
