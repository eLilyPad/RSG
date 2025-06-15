using Godot;

namespace RSG.UI;

public sealed partial class TilesContainer : GridContainer
{
	public interface IHandleButtonPress
	{
		void OnButtonPressed(Vector2I position, Button button);
		void OnButtonPressed(Nonogram.PenMode mode, Vector2I position, Button button)
		{
			button.Text = mode switch
			{
				Nonogram.PenMode.Block when button.Text is EmptyText or FillText => BlockText,
				Nonogram.PenMode.Block => EmptyText,
				Nonogram.PenMode.Fill when button.Text is EmptyText => BlockText,
				Nonogram.PenMode.Fill when button.Text is FillText => EmptyText,
				_ => button.Text
			};
		}
	}

	public static TilesContainer Create(IHandleButtonPress TilePressedHandler, Nonogram.DisplaySettings displaySettings)
	{
		var tilesContainer = new TilesContainer(TilePressed: TilePressedHandler)
		{
			Name = "Tiles",
			Columns = displaySettings.Length,
			Size = displaySettings.TileSize,
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: displaySettings.Margin
		);

		return tilesContainer;
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Action<Vector2I, Button> OnButtonPressed { get; }
	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	private TilesContainer(IHandleButtonPress TilePressed)
	{
		Name = nameof(TilesContainer);
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		OnButtonPressed = TilePressed.OnButtonPressed;
	}
	public override void _Ready()
	{
		foreach (Vector2I position in (Vector2I.One * Columns).AsRange())
		{
			var button = Buttons[position] = new Button
			{
				Name = $"Button {position}",
				Text = EmptyText,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			}.AnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
			AddChild(button);

			button.Pressed += () => OnButtonPressed(position, button);
		}
	}
}
