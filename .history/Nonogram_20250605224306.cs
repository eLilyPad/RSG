using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node, IHavePenMode
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
	public GameContainer PainterDisplay { get => field ??= new(nonogram: this); private set; }

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(GameDisplay, PainterDisplay);
	}

	public readonly record struct DisplaySettings(int Length = 5, int Scale = 40, int Margin = 150)
	{
		public readonly Vector2I Size => Vector2I.One * Length;
		public readonly Vector2I TileSize => Size * Scale;
		public readonly Vector2I BackgroundSize => Size * (Scale + 5);
	}
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay<HintsContainer>
	{
		public override DisplaySettings Settings { get; set; } = nonogram.Settings;
		public override GridContainer Tiles => field ??= new GridContainer
		{
			Columns = nonogram.Settings.Length,
			Size = nonogram.Settings.TileSize
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		public override (HintsContainer Rows, HintsContainer Columns) Hints { get; } = HintsContainer.Hints(
			length: nonogram.Settings.Length
		);
		public override ColorRect Background { get; } = ColouredBackground(
			colours: nonogram.Colours,
			displaySettings: nonogram.Settings
		);

		public override void OnTilePressed(Vector2I position, Button button) => button.Text = nonogram.CurrentPenMode switch
		{
			PenMode.Block when button.Text is EmptyText or FillText => BlockText,
			PenMode.Block => EmptyText,
			PenMode.Fill when button.Text is EmptyText => BlockText,
			PenMode.Fill when button.Text is FillText => EmptyText,
			_ => button.Text
		};
	}
}