using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node, IHavePenMode, TilesContainer.IHandleTiles
{
	public enum Type { Game, Painter }
	public enum PenMode { Block, Fill }

	public required ColourPack Colours { get; init; }

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
	public GameContainer GameDisplay { get => field ??= new(nonogram: this); private set; }
	public PaintingContainer PainterDisplay { get => field ??= new(nonogram: this); private set; }

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
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay<TilesContainer, HintsContainer>
	{
		public override (HintsContainer Rows, HintsContainer Columns) Hints { get; } = HintsContainer.Hints(
			length: nonogram.Settings.Length
		);
		public override TilesContainer Tiles { get; } = TilesContainer.Create(
			TilePressedHandler: nonogram,
			displaySettings: nonogram.Settings
		);
		public override ColorRect Background { get; } = ColouredBackground(
			colours: nonogram.Colours,
			displaySettings: nonogram.Settings
		);
	}
	public sealed partial class PaintingContainer(Nonogram nonogram) : NonogramDisplay<TilesContainer, HintsContainer>
	{
		public override (HintsContainer Rows, HintsContainer Columns) Hints { get; } = HintsContainer.Hints(
			length: nonogram.Settings.Length
		);
		public override TilesContainer Tiles { get; } = TilesContainer.Create(
			TilePressedHandler: nonogram,
			displaySettings: nonogram.Settings
		);
		public override ColorRect Background { get; } = ColouredBackground(
			colours: nonogram.Colours,
			displaySettings: nonogram.Settings
		);
	}
}