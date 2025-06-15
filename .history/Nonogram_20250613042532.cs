using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node, IHavePenMode
{
	public enum Type { Game, Painter }
	public enum PenMode { Block, Fill }

	public required ColourPack Colours { get; init; }

	public PuzzleData CurrentData { get; private set; } = new();
	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
	public DisplayConfig Settings
	{
		get; set
		{
			field = value;
			GameDisplay.UpdateSettings(Colours, field);
			PainterDisplay.UpdateSettings(Colours, field);
		}
	} = new();

	public GameContainer GameDisplay => field ??= new(nonogram: this) { TilesLength = 5 };
	public PaintContainer PainterDisplay => field ??= new(nonogram: this) { TilesLength = 5 };

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(
			//GameDisplay, 
			PainterDisplay
		);
	}

	public sealed record PuzzleData
	{
		private readonly Dictionary<Vector2I, bool> _checkedTiles = [];

		public void Change(Vector2I position, bool clicked)
		{
			_checkedTiles[position] = clicked;
		}

		public Dictionary<NonogramDisplay.TileHints.Position, List<int>> Hints()
		{
			Dictionary<NonogramDisplay.TileHints.Position, List<int>> hints = [];
			foreach ((Vector2I position, bool check) in _checkedTiles)
			{
				var col = hints[new(NonogramDisplay.TileHints.Side.Column, position.X)] = [];
				var row = hints[new(NonogramDisplay.TileHints.Side.Row, position.Y)] = [];
				if (check)
				{
					col.Add(0);
					row.Add(0);
				}
				else
				{
					col.Add(1);
					row.Add(1);
				}

			}

			return hints;

		}
	}

	public readonly record struct DisplayConfig(int Length = 5, int Scale = 40, int Margin = 150) : NonogramDisplay.IConfig
	{
		public readonly Vector2I Size => Vector2I.One * Length;
		public readonly Vector2I TilesSize => Size * Scale;
		public readonly Vector2I BackgroundSize => Size * (Scale + 5);
	}
	public sealed partial class PaintContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override void OnTilePressed(Button button, Vector2I position)
		{
			button.Text = nonogram.CurrentPenMode switch
			{
				PenMode.Block when button.Text is EmptyText or FillText => BlockText,
				PenMode.Block => EmptyText,
				PenMode.Fill when button.Text is EmptyText => BlockText,
				PenMode.Fill when button.Text is FillText => EmptyText,
				_ => button.Text
			};
			nonogram.CurrentData.Change(position, button.Text is FillText);
			Hints.WriteToLabels(hints: nonogram.CurrentData.Hints());
		}
	}
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override void OnTilePressed(Button button, Vector2I position)
		{
			button.Text = nonogram.CurrentPenMode switch
			{
				PenMode.Block when button.Text is EmptyText or FillText => BlockText,
				PenMode.Block => EmptyText,
				PenMode.Fill when button.Text is EmptyText => BlockText,
				PenMode.Fill when button.Text is FillText => EmptyText,
				_ => button.Text
			};
		}
	}
}