using Godot;

namespace RSG.UI;

public sealed partial class TilesContainer : GridContainer
{
	public interface IHandleButtonPress
	{
		void OnButtonPressed(Vector2I position, Button button);
		void OnButtonPressed(Core.PenMode mode, Vector2I position, Button button)
		{
			button.Text = mode switch
			{
				Core.PenMode.Block when button.Text is EmptyText or FillText => BlockText,
				Core.PenMode.Block => EmptyText,
				Core.PenMode.Fill when button.Text is EmptyText => BlockText,
				Core.PenMode.Fill when button.Text is FillText => EmptyText,
				_ => button.Text
			};
		}
	}

	public static TilesContainer Create<T>(
		T data,
		(int length, int scale, int margin) displaySettings
	)
	where T : IHavePenMode, IHaveColourPack, IHandleButtonPress
	{
		var tilesContainer = new TilesContainer
		{
			Name = "Tiles",
			Columns = displaySettings.length,
			Size = Vector2I.One * displaySettings.length * displaySettings.scale,
			OnButtonPressed = data.OnButtonPressed,
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.BottomRight,
			resizeMode: LayoutPresetMode.KeepSize,
			margin: displaySettings.margin
		);

		return tilesContainer;
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public required Action<Vector2I, Button> OnButtonPressed { get; init; }
	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	private TilesContainer() { }
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
