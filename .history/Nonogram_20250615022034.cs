using Godot;

namespace RSG;

using static UI.NonogramDisplay;
using UI;

public partial class Nonogram : Container
{
	public enum Type { Game, Painter }

	public DisplayConfig Config { get; init; } = new(5, 1, 1);
	public PuzzleData CurrentData
	{
		get; set
		{
			PainterDisplay.WriteToLabels(value);
			GameDisplay.WriteToLabels(value);
			field = value;
		}
	}
	public required ColourPack Colours { get; init => GameDisplay.Colours = PainterDisplay.Colours = field = value; }

	public GameContainer GameDisplay => field ??= new(nonogram: this) { Name = "Game" };
	public PaintContainer PainterDisplay => field ??= new(nonogram: this) { Name = "Painter" };
	public TabContainer DisplayTabs { get; } = new TabContainer
	{
		TabsVisible = true
	}
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public Nonogram() { CurrentData = new(Config); }

	public override void _Ready()
	{
		this.Add(DisplayTabs.Add(GameDisplay, PainterDisplay))
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		CurrentData = new(Config);
	}

	public readonly record struct DisplayConfig(int Length, int Scale, int Margin) : IConfig;
	public sealed record PuzzleData : IData
	{
		private readonly Dictionary<Vector2I, bool> _tiles = [];
		public PuzzleData(IConfig config)
		{
			foreach (Vector2I position in (Vector2I.One * config.Length).AsRange())
			{
				_tiles[position] = false;
			}
		}
		public void Change(Vector2I position, bool clicked)
		{
			_tiles[position] = clicked;
		}

		public Dictionary<HintPosition, List<int>> Hints()
		{
			Dictionary<HintPosition, List<int>> hints = [];

			foreach ((Vector2I position, bool check) in _tiles)
			{
				(HintPosition rowPos, HintPosition columnPos) = HintPosition.ToPosition(position);

				if (!hints.TryGetValue(rowPos, out var row)) { hints[rowPos] = row = []; }
				if (!hints.TryGetValue(columnPos, out var column)) { hints[columnPos] = column = []; }

				if (check)
				{
					switch (column.Count)
					{
						case not 0 when column[^1] >= 1: ++column[^1]; break;
						default: column.Add(1); break;
					}
					switch (row.Count)
					{
						case not 0 when row[^1] >= 1: ++row[^1]; break;
						default: row.Add(1); break;
					}
				}
				else
				{
					column.Add(0);
					row.Add(0);
				}
			}
			foreach ((HintPosition position, List<int> hintsNumber) in hints)
			{
				hints[position] = [.. hintsNumber.Where(i => i is not 0)];
			}

			return hints;
		}
	}
	public sealed partial class PaintContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override IConfig Config => nonogram.Config;
		public override void OnTilePressed(Button button, Vector2I position)
		{
			base.OnTilePressed(button, position);
			nonogram.CurrentData.Change(position, clicked: button.Text is FillText);
			WriteToLabels(nonogram.CurrentData);
		}
	}
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override IConfig Config => nonogram.Config;
	}
}