using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node, IHavePenMode, TilesContainer.IHandleTiles
{
	public enum Type { Game, Painter }
	public enum PenMode { Block, Fill }

	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
	public DisplaySettings Settings
	{
		get; set
		{
			field = value;
			GameDisplay.Free();
			PainterDisplay.Free();
			(GameDisplay, PainterDisplay) = (null, null);
			this.Add(GameDisplay, PainterDisplay);
		}
	}
	public NonogramContainer GameDisplay { get => field ??= NonogramContainer.Game(this); private set; }
	public NonogramContainer PainterDisplay { get => field ??= NonogramContainer.Painting(this); private set; }

	public Button LastPressed => throw new NotImplementedException();

	public required ColourPack Colours { get; init; }

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(GameDisplay, PainterDisplay);
	}
	public void OnButtonPressed(Vector2I position, Button button)
	{
		(this as TilesContainer.IHandleTiles).OnButtonPressed(position, button, CurrentPenMode);
	}

	public readonly record struct DisplaySettings(int Length = 5, int Scale = 40, int Margin = 150)
	{
		public readonly Vector2I Size => Vector2I.One * Length;
		public readonly Vector2I TileSize => Size * Scale;
		public readonly Vector2I BackgroundSize => Size * (Scale + 5);
	}

	public sealed partial class GameContainer : NonogramDisplay
	{
		private GameContainer(Nonogram nonogram)
		{
			Background = ColouredBackground(nonogram.Colours, nonogram.Settings);
			Tiles = TilesContainer.Create(nonogram, nonogram.Settings);
			Hints = HintsContainer.Hints(nonogram.Settings.Length);
		}
	}
	private sealed partial class PaintingContainer : NonogramDisplay { }
}