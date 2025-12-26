using Godot;

namespace RSG.Nonogram;

public sealed partial class Tile : PanelContainer
{
	public static Tile Create(Vector2I position, IColours colours, Action<Vector2I> pressed, Action<bool> mouseState)
	{
		Tile tile = new Tile { Name = $"Tile (X: {position.X}, Y: {position.Y})" }
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		tile.ResetStyle(position, colours);
		tile.Button.AddThemeColorOverride("font_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_hover_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_focus_color", Colors.Transparent);
		tile.Button.AddThemeColorOverride("font_pressed_color", Colors.Transparent);
		tile.Button.AddThemeFontSizeOverride("font_size", 10);
		tile.Resized += () => tile.Button.PivotOffset = tile.Button.Size / 3;
		tile.Button.MouseExited += () => mouseState(false);
		tile.Button.ButtonDown += () => pressed(position);
		tile.Button.MouseEntered += () =>
		{
			mouseState(true);
			bool fill = Input.IsMouseButtonPressed(Display.FillButton);
			bool block = Input.IsMouseButtonPressed(Display.BlockButton);
			if (!fill && !block) { return; }
			pressed(position);
		};

		return tile;
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;
	public Button Button { get; } = new Button { Text = Display.EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	public override void _Ready() => this.Add(Button);

	public void ResetStyle(Vector2I position, IColours colours)
	{
		const int chunkSize = 5;
		StyleBox baseBox = Button.GetThemeStylebox("normal");
		if (baseBox.Duplicate() is not StyleBoxFlat style) return;
		int chunkIndex = position.X / chunkSize + position.Y / chunkSize;
		Color filledTile = colours.NonogramTileBackgroundFilled;
		Color background = chunkIndex % 2 == 0 ? colours.NonogramTileBackground1 : colours.NonogramTileBackground2;
		Color blocked = background.Darkened(.4f);

		style.BgColor = Button.Text switch
		{
			Display.FillText => filledTile,
			Display.BlockText => blocked,
			_ => background
		};
		style.CornerDetail = 1;
		style.SetCornerRadiusAll(0);

		Button.AddThemeStyleboxOverride("normal", style);
	}
}
