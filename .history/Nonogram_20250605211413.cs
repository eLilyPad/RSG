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
	public NonogramDisplay GameDisplay { get => field ??= new GameContainer(this); private set; }
	public NonogramDisplay PainterDisplay { get => field ??= new PaintingContainer(this); private set; }

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
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override (HintsContainer Rows, HintsContainer Columns) Hints { get; } = HintsContainer.Hints(
			nonogram.Settings.Length
		);
		public override TilesContainer Tiles { get; } = TilesContainer.Create(
			nonogram,
			nonogram.Settings
		);
		public override ColorRect Background { get; } = new ColorRect
		{
			Name = "Background",
			Color = nonogram.Colours.NonogramBackground,
			Size = nonogram.Settings.BackgroundSize
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: nonogram.Settings.Margin
		);
	}
	public sealed partial class PaintingContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override (HintsContainer Rows, HintsContainer Columns) Hints { get; } = HintsContainer.Hints(
			nonogram.Settings.Length
		);
		public override TilesContainer Tiles { get; } = TilesContainer.Create(
			nonogram,
			nonogram.Settings
		);
		public override ColorRect Background { get; } = new ColorRect
		{
			Name = "Background",
			Color = nonogram.Colours.NonogramBackground,
			Size = nonogram.Settings.BackgroundSize
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize,
			nonogram.Settings.Margin
		);
	}
	//private sealed partial class PaintingContainer : NonogramDisplay { }
}