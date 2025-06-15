using Godot;

namespace RSG;

using UI;

public partial class Nonogram : Node, IHavePenMode
{
	public enum Type { Game, Painter }
	public enum PenMode { Block, Fill }

	public required ColourPack Colours { get; init; }

	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
	public DisplayConfig Settings
	{
		get; set
		{
			field = value;
			GameDisplay.UpdateSettings();
			PainterDisplay.UpdateSettings();
		}
	} = new();

	public GameContainer GameDisplay => field ??= new(nonogram: this);
	public GameContainer PainterDisplay => field ??= new(nonogram: this);

	public (int Columns, Vector2 Size) TilesConfig => (Settings.Length, Settings.TilesSize);
	public (Color Colour, Vector2 Size) BackgroundConfig => (Colours.NonogramBackground, Settings.TilesSize);

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(GameDisplay, PainterDisplay);
	}

	public readonly record struct DisplayConfig(int Length = 5, int Scale = 40, int Margin = 150)
	{
		public readonly Vector2I Size => Vector2I.One * Length;
		public readonly Vector2I TilesSize => Size * Scale;
		public readonly Vector2I BackgroundSize => Size * (Scale + 5);
	}
	public sealed partial class GameContainer(Nonogram nonogram) : NonogramDisplay
	{
		public override void OnTilePressed(Vector2I position, Button button) => button.Text = nonogram.CurrentPenMode switch
		{
			PenMode.Block when button.Text is EmptyText or FillText => BlockText,
			PenMode.Block => EmptyText,
			PenMode.Fill when button.Text is EmptyText => BlockText,
			PenMode.Fill when button.Text is FillText => EmptyText,
			_ => button.Text
		};
		public override void UpdateSettings()
		{
			if (!IsNodeReady())
			{
				Ready += Update;
			}
			else
			{
				Update();
			}

			void Update()
			{
				(BackgroundRect.Color, BackgroundRect.Size) = nonogram.BackgroundConfig;
				BackgroundRect.SetAnchorsAndOffsetsPreset(
					preset: LayoutPreset.FullRect,
					resizeMode: LayoutPresetMode.KeepSize,
					margin: nonogram.Settings.Margin
				);

				(Tiles.Columns, Tiles.Size) = nonogram.TilesConfig;

			}
		}
	}
}