using Godot;
using RSG.UI;
namespace RSG.Nonogram;

using static Display;
using Mode = Display.TileMode;

public abstract partial class Display
{
	public enum Side : byte { Row = 0, Column = 1 }
	public static Dictionary<Vector2I, Mode> CreateTiles(Func<Vector2I, bool> selector, int size) => (Vector2I.One * size)
		.GridRange()
		.ToDictionary(elementSelector: position => selector(position) ? Mode.Filled : Mode.Clear);
	public static Dictionary<Vector2I, Mode> CreateTiles(int size) => (Vector2I.One * size)
		.GridRange()
		.ToDictionary(elementSelector: _ => Mode.Clear);
	public static ulong[][] DataMask(Side side, Data data)
	{
		int size = data.Size;
		ulong[][] lines = new ulong[size][];
		for (int i = 0; i < data.Size; i++)
		{
			Stack<MaskGen> maskStack = new([MaskGen.Empty]);
			List<ulong> possibilities = [];
			bool noHints = !data.Hints.HasHint(new(side, i));
			if (noHints)
			{
				possibilities.Add(MaskGen.Null);
				lines[i] = [.. possibilities];
				continue;
			}
			while (maskStack.Count > 0)
			{
				MaskGen gen = maskStack.Pop();
				(int id, int position, ulong mask) = gen;
				HintPosition otherPosition = new(side, position);
				if (!LineMaskMatchesPlayerState(mask, i)) continue;
				possibilities.Add(mask);
				data.Hints.GetRemaining(otherPosition, id, out int remainingMin);
				data.Hints.TotalHints(otherPosition, out int total);
				data.Hints.HintAt(otherPosition, id, out int block);
				for (int start = size - block; start >= position; start--)
				{
					if (start + block + remainingMin > size) continue;
					maskStack.Push(gen.Next(start, total, block));
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

		public const string DefaultName = "Puzzle";
		public const int DefaultSize = 15;
		public Action<Vector2I>? Modified { get; set; }
		public virtual string Name { get; set; } = DefaultName;
		public abstract Dictionary<Vector2I, Mode> Tiles { protected get; init; }

		public IPuzzleHints Hints => _hints;
		public IImmutableDictionary<Vector2I, Mode> States => Tiles.ToImmutableDictionary();
		public IEnumerable<HintPosition> HintPositions => Tiles.Keys.SelectMany(
			key => HintPosition.Convert(key)
		);
		public bool IsSquare => _isTilesSquare ??= Tiles.IsSquare<Dictionary<Vector2I, Mode>, Mode>();
		public virtual int Size => (int)Mathf.Sqrt(Tiles.Count);
		protected ulong[][] RowMasks => field ??= DataMask(Side.Row, this);
		protected ulong[][] ColumnMasks => field ??= DataMask(Side.Column, this);
		private bool? _isTilesSquare;

		private readonly ExpectedHints _hints;
		public Data(int size = DefaultSize)
		: this(name: DefaultName, values: CreateTiles(size)) { }
		public Data(string name, Func<Vector2I, bool> selector, int size)
		: this(name, values: CreateTiles(selector, size)) { }
		public Data(Data data)
		: this(name: data.Name, values: data.Tiles) { }
		public Data(string name, Dictionary<Vector2I, Mode> values)
		{
			Name = name;
			Tiles = values;
			_hints = new(this);
		}
		public ImageTexture AsIcon(IColours colours, int pixelSize = 16) => Tiles
			.AsIcon(getColor: colours.ChequeredNonogramTile, size: Size, pixelSize)
			.ToImage();

		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(HintPosition position)
			=> InLine(position: position.Origin, side: position);
		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(int index, Side side)
			=> InLine(position: side.Direction() * index, side);
		public IEnumerable<(Vector2I Position, Mode Mode)> InLine(Vector2I position, Side side)
		{
			int id = side.IndexFrom(position);
			foreach ((Vector2I key, Mode mode) in Tiles)
			{
				int otherID = side.IndexFrom(key);
				if (id != otherID) continue;
				yield return (key, mode);
			}
		}
		public IEnumerable<(Vector2I Position, Mode Mode)> InLines(Vector2I position)
		{
			foreach (var value in InLine(position, Side.Row)) yield return value;
			foreach (var value in InLine(position, Side.Column)) yield return value;
		}
		internal virtual void ChangeState(Vector2I position, Mode mode)
		{
			Assert(Tiles.ContainsKey(position), "given position is not already in the base dictionary");
			Tiles[position] = mode;
			Hints.Recalculate(position);
			Modified?.Invoke(position);
		}
	}
	public sealed record ExpectedHints(Data Save) : IPuzzleHints
	{
		public int[][] Rows => field ??= FilledLines(Side.Row, Save);
		public int[][] Columns => field ??= FilledLines(Side.Column, Save);

		private readonly StringBuilder builder = new();

		public void Recalculate(Vector2I position)
		{
			(HintPosition row, HintPosition column) = HintPosition.ConvertVector(position);
			Recalculate(row);
			Recalculate(column);
		}
		public void Recalculate(HintPosition position)
		{
			int[][] hints = Get(position);
			AssertIndex(hints, position);
			Save.InLine(position)
				.Select(pair => pair.Mode is Mode.Filled ? 1 : 0)
				.Condense(out hints[position.Index]);
		}
		public bool HasHint(HintPosition position) => GetLine(position).Length is 0;
		public void GetRemaining(HintPosition position, int index, out int value) => value = GetLine(position).Remaining(index);
		public void HintAt(HintPosition position, int index, out int value) => value = GetHint(position, index);
		public string TextLineAt(HintPosition position)
		{
			builder.Clear();
			int connected = 0;
			foreach ((Vector2I _, Mode Mode) in Save.InLine(position))
			{
				int filled = Mode is Mode.Filled ? 1 : 0;
				int prev = connected;
				connected = (connected + filled) * filled;
				if (prev > 0 && filled == 0) AppendHint(prev);
			}
			if (connected > 0) AppendHint(connected);
			return builder.Length > 0 ? builder.ToString() : EmptyHint;

			void AppendHint(int value) => builder
				.Append(value)
				.Append(position.Format);
		}
		public void TotalHints(HintPosition position, out int value) => value = GetLine(position).Length;
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
		private int[][] Get(Side side) => side switch { Side.Row => Rows, _ => Columns };
	}
	private readonly record struct MaskGen(int Index, int Position, ulong Mask)
	{
		public const ulong Null = 0UL;
		public static MaskGen Empty = new(0, 0, Null);
		public readonly MaskGen Next(int start, int total, int block)
		{
			int nextPosition = start + block;
			ulong newMask = Mask;
			for (int nextI = 0; nextI < block; nextI++) newMask |= 1UL << (start + nextI);
			if (Index + 1 < total) nextPosition++;
			MaskGen gen = new(Index + 1, nextPosition, newMask);
			return gen;
		}
	}
}
