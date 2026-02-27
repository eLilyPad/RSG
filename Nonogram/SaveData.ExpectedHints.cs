using Godot;

namespace RSG.Nonogram;

using static Display;
using Mode = Display.TileMode;
public sealed partial class SaveData
{
	private record ExpectedHints(SaveData Save) : IPuzzleHints
	{
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
			hints[position.Index] = Line(Save, position);
		}
		public string TextLineAt(HintPosition position)
		{
			builder.Clear();
			int[] line = GetLine(position);
			string format = position.Side.AsFormat();
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
		//private bool IsHintCorrect(HintPosition position, int index) { }
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private int[][] Get(Side side) => side switch { Side.Row => Save.Rows, _ => Save.Columns };
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
	}
	private static int[] Line(SaveData save, HintPosition position)
	{
		int[] value = save
			.InLine(position)
			.Select(pair => pair.Mode is Mode.Filled ? 1 : 0)
			.Condense();
		return value is { Length: 0 } ? [0] : value;
	}
	public static int[][] Lines(SaveData Save, Side Side)
	{
		int[][] values = new int[Save.Size][];
		foreach (var id in Save.Line())
		{
			values[id] = Line(Save, new(Side, id));
		}
		return values;
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


	//private record HintLine(SaveData Save, HintPosition Position)
	//{
	//	public string Format = Position.Side.AsFormat();
	//	public int TotalFilled => _values.Sum();
	//	private readonly (int start, int end)[] _voids = CreateVoids(Save, Position);
	//	private readonly int[] _values = CreateValues(Save, Position);
	//	public override string ToString() => _values switch
	//	{
	//		{ Length: > 0 } => FormatHints(),
	//		_ => EmptyHint + Format
	//	};

	//	private static int[] CreateValues(SaveData Save, HintPosition position) => Save
	//		.InLine(position)
	//		.OrderBy(keySelector: pair => position.Side.OrderFrom(position: pair.Key))
	//		.Select(pair => pair.Value is Mode.Filled ? 1 : 0)
	//		.Condense();
	//	private static (int start, int end)[] CreateVoids(Data save, HintPosition position)
	//	{
	//		List<(int start, int end)> values = [];
	//		int voidId = 0;
	//		Vector2I prev = Vector2I.Zero;
	//		foreach ((Vector2I Position, Mode Mode) in save.InLine(position))
	//		{
	//			position.Side.Shift(ref prev, -1);
	//			int id = position.Side.OrderFrom(Position);
	//			if (Mode is Mode.Filled)
	//			{
	//				if (save.States.TryGetValue(prev, out Mode prevMode))
	//				{

	//				}
	//			}

	//		}

	//		return [.. values];
	//	}
	//	private string FormatHints()
	//	{
	//		StringBuilder builder = new();
	//		for (int i = 0; i < _values.Length; i++)
	//		{
	//			int value = _values[i];
	//			builder.Append(value).Append(value: Format);
	//		}
	//		return builder.ToString();
	//	}
	//}

}
