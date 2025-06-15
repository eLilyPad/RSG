using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node
{
	public enum Type { Game, Painter }

	public DisplayConfig Config { get; init; } = new();
	public PuzzleData CurrentData
	{
		get; private set
		{
			PainterDisplay.WriteToLabels(value);
			GameDisplay.WriteToLabels(value);
		}
	} = new();
	public required ColourPack Colours { get; init => GameDisplay.Colours = PainterDisplay.Colours = field = value; }

	public GameContainer GameDisplay => field ??= new(nonogram: this) { Name = "Game" };
	public PaintContainer PainterDisplay => field ??= new(nonogram: this) { Name = "Painter" };

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(
			//GameDisplay, 
			PainterDisplay
		);
		PainterDisplay.WriteToLabels(CurrentData);
	}

	public readonly record struct DisplayConfig(int Length = 5, int Scale = 40, int Margin = 150) : NonogramDisplay.IConfig;
	public sealed record PuzzleData : NonogramDisplay.IData
	{
		private readonly Dictionary<Vector2I, bool> _checkedTiles = [];

		public void Change(Vector2I position, bool clicked)
		{
			_checkedTiles[position] = clicked;
		}

		public Dictionary<NonogramDisplay.HintPosition, List<int>> Hints()
		{
			Dictionary<NonogramDisplay.HintPosition, List<int>> hints = [];
			foreach ((Vector2I position, bool check) in _checkedTiles)
			{
				NonogramDisplay.HintPosition rowPos = new(NonogramDisplay.Side.Row, position.X),
					columnPos = new(NonogramDisplay.Side.Column, position.Y);

				if (!hints.TryGetValue(rowPos, out var row)) { hints[rowPos] = row = []; }
				if (!hints.TryGetValue(columnPos, out var column)) { hints[columnPos] = column = []; }

				if (check)
				{
					column.Add(1);
					row.Add(1);
				}
				else
				{
					column.Add(0);
					row.Add(0);
				}
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