using Godot;

namespace RSG.Nonogram;


using static Display;
using Mode = Display.TileMode;

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
		public static Dictionary<Vector2I, Mode> CreateTiles(int size) => (Vector2I.One * size)
			.GridRange().ToDictionary(elementSelector: _ => Mode.Clear);
		public static ulong[][] DataMask(Side side, Data data)
		{
			const ulong nothing = 0UL;
			int size = data.Size;
			ulong[][] lines = new ulong[size][];
			int[][] hintLines = side switch { Side.Row => data.Hints.Rows, _ => data.Hints.Columns };
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
				foreach ((Vector2I pos, Mode mode) in data.InLine(index, side))
				{
					bool filled = ((mask >> side.OrderFrom(pos)) & 1UL) != 0;
					if (mode == Mode.Filled && !filled) return false;
					if (mode == Mode.Blocked && filled) return false;
				}
				return true;
			}
		}
		public static int[][] FilledLines(Side side, Data data)
		{
			int[][] values = new int[data.Size][];
			for (int i = 0; i < data.Size; i++)
			{
				HintPosition position = new(side, i);
				values[i] = Line(data, position);
			}
			return values;
		}
		public static int[] Line(Data save, HintPosition position)
		{
			save.InLine(position)
				.Select(pair => pair.Mode is Mode.Filled ? 1 : 0)
				.Condense(out int[] value);
			return value is { Length: 0 } ? [0] : value;
		}

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public Action<Vector2I>? Modified { get; set; }
		public virtual string Name { get; set; } = DefaultName;
		public abstract Dictionary<Vector2I, Mode> Tiles { protected get; init; }

		public bool IsSquare => _isTilesSquare ??= Tiles.IsSquare<Dictionary<Vector2I, Mode>, Mode>();
		public IImmutableDictionary<Vector2I, Mode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		protected ulong[][] RowMasks => field ??= DataMask(Side.Row, this);
		protected ulong[][] ColumnMasks => field ??= DataMask(Side.Column, this);
		private bool? _isTilesSquare;
		protected ExpectedHints Hints => field ??= new(this);

		public Data(int size = DefaultSize) { Tiles = CreateTiles(size); }
		public Data(string name, Func<Vector2I, bool> selector, int size)
		{
			Name = name;
			Tiles = (Vector2I.One * size).GridRange().ToDictionary(
				elementSelector: position => selector(position) ? Mode.Filled : Mode.Clear
			);
		}
		public IEnumerable<int> Line() { for (int i = 0; i < Size; i++) yield return i; }

		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(HintPosition position)
			=> InLine(position: position.Origin, side: position);
		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(int index, Side side)
			=> InLine(position: side.Origin() * index, side);
		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(Vector2I position, Side side)
		{
			const int step = 1;
			side.Shift(ref position, amount: -side.OrderFrom(position));
			for (int i = 0; i < Size; i += step)
			{
				side.Shift(ref position, step);
				if (!Tiles.TryGetValue(position, out Mode mode)) continue;
				yield return (position, mode);
			}
		}
		public IEnumerable<(Vector2I Position, Mode Mode)> InLines(Vector2I position)
		{
			foreach (var value in InLine(position, Side.Row)) yield return value;
			foreach (var value in InLine(position, Side.Column)) yield return value;
		}
		internal void ChangeState(Vector2I position, Mode mode)
		{
			Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
			Tiles[position] = mode;
			Modified?.Invoke(position);
		}

		protected sealed record ExpectedHints(Data Save) : IPuzzleHints
		{
			public int[][] Rows => field ??= FilledLines(Side.Row, Save);
			public int[][] Columns => field ??= FilledLines(Side.Column, Save);

			private readonly StringBuilder builder = new();

			public void Recalculate(Vector2I position)
			{
				(HintPosition row, HintPosition column) = HintPosition.ConvertVector(position);
				Recalculate(row, column);
			}
			public void Recalculate(params ReadOnlySpan<HintPosition> positions)
			{
				foreach (HintPosition position in positions) Recalculate(position);
			}
			public void Recalculate(HintPosition position)
			{
				int[][] hints = Get(position);
				AssertIndex(hints, position);
				Save.InLine(position)
					.Select(pair => pair.Mode is Mode.Filled ? 1 : 0)
					.Condense(out hints[position.Index]);
			}
			public string TextLineAt(HintPosition position)
			{
				builder.Clear();
				int[] line = GetLine(position);
				string format = position.Format;
				for (int id = 0; id < line.Length; id++)
				{
					int hint = line[id];
					builder.Append(hint).Append(format);
				}
				return builder.ToString();
			}
			public string TextLineAt(HintPosition position, int index)
			{
				builder.Clear();
				return builder
					.Append(GetHint(position, index))
					.Append(position.Side.AsFormat())
					.ToString();
			}
			private int GetHint(HintPosition position, int index)
			{
				int[][] hints = Get(position);
				AssertIndex(hints, position, index);
				return hints[position.Index][index];
			}
			private int[] GetLine(HintPosition position)
			{
				int[][] hints = Get(position);
				AssertIndex(hints, position);
				return hints[position.Index];
			}
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private int[][] Get(Side side) => side switch { Side.Row => Rows, _ => Columns };
		}
	}
	private static bool AssertIndex(int[][] values, HintPosition position, int index)
	{
		bool hasIndex = values.Length > position.Index;
		Assert(condition: hasIndex, $"no hint present index {position.Index}, larger that puzzle size {values.Length}");
		AssertIndex(values[position.Index], index);
		return hasIndex;
	}
	private static bool AssertIndex(int[][] values, HintPosition position)
	{
		bool hasIndex = values.Length > position.Index;
		Assert(condition: hasIndex, $"no hint present index {position.Index}, larger that puzzle size {values.Length}");
		return hasIndex;
	}
	private static bool AssertIndex(int[] values, int index)
	{
		bool hasIndex = values.Length > index;
		Assert(condition: hasIndex, $"no hint present index {index}, larger that puzzle size {values.Length}");
		return hasIndex;
	}
	public enum Side : byte { Row = 0, Column = 1 }
}
