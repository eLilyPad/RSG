using Godot;

namespace RSG.Nonogram;

using static Display;

public sealed partial class Tile : PanelContainer
{
	public interface ILocker
	{
		IImmutableList<Func<Vector2I, bool>> Rules { get; }
		bool ShouldLock(Vector2I position)
		{
			foreach (Func<Vector2I, bool> rule in Rules)
			{
				if (rule(position)) return true;
			}
			return false;
		}
	}
	public interface ISize { Vector2 TileSize { get; set; } }
	public interface IProvider
	{
		Node Parent();
		void OnActivate(Vector2I position, Tile tile) { }
		TileMode State(Vector2I position) => TileMode.Clear;
	}

	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public static Tile CreatePooled(
		Vector2I position,
		IColours colours,
		Action<bool> hover,
		Action<Vector2I, Tile> activate
	)
	{
		Tile tile = new Tile
		{
			Name = $"Tile (X: {position.X}, Y: {position.Y})",
			Colours = colours,
			Mode = TileMode.Clear,
		}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);


		tile.Resized += () => tile.Button.PivotOffset = tile.Button.Size / 2;
		tile.Button.ButtonDown += () => activate(position, tile);
		tile.Button.MouseExited += () => hover(false);
		tile.Button.MouseEntered += () =>
		{
			activate(position, tile);
			hover(true);
		};

		tile.Button
			.OverrideStyle(modify: (StyleBoxFlat style) =>
			{
				style.CornerDetail = 1;
				style.SetCornerRadiusAll(0);
				return style;
			})
			.OverrideStyle(name: "hover", modify: (StyleBoxFlat style) =>
			{
				style.CornerDetail = 1;
				style.SetCornerRadiusAll(0);
				style.BgColor = Colors.Transparent;
				return style;
			})
			.OverrideStyle(name: "focus", modify: (StyleBox style) => new StyleBoxEmpty())
			.AddAllFontThemeOverride(Colors.Transparent);
		tile.Button.AddThemeFontSizeOverride("font_size", 10);

		return tile;
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

	public bool IsAlternative { get; set => ChangeAlternative(field = value); } = false;
	public required IColours Colours { private get; set; }
	[Export] public bool Locked { get; set => ChangeLocked(field = value); } = false;
	[Export] public bool Hovering { get; set => ChangeHovering(field = value); } = false;
	[Export] public TileMode Mode { get; set => ChangeMode(field = value); } = TileMode.NULL;

	private Tile() { }
	public override void _Ready() => this.Add(Button);

	private void ChangeHovering(bool value) => Button.Scale = Vector2.One * (value ? .9f : 1);
	private void ChangeAlternative(bool value)
	{
		Button.OverrideStyle(modify: (StyleBoxFlat style) =>
		{
			style.BgColor = Colours.NonogramTileBackground(mode: Mode, alternative: IsAlternative);
			return style;
		});
		Button.OverrideStyle(name: "hover", modify: (StyleBoxFlat style) =>
		{
			style.BgColor = Colours.NonogramTileBackground(mode: Mode, alternative: IsAlternative);

			return style;
		});
	}
	private void ChangeLocked(bool value)
	{
		Button.OverrideStyle((StyleBoxFlat style) =>
		{
			style.SetBorderWidthAll(value ? 2 : 0);
			return style;
		});
	}
	private void ChangeMode(TileMode value)
	{
		Button.OverrideStyle(modify: (StyleBoxFlat style) =>
		{
			style.BorderColor = Colours.NonogramLockedBorder(value);
			style.BgColor = Colours.NonogramTileBackground(mode: value, alternative: IsAlternative);
			return style;
		});
		Button.OverrideStyle(name: "hover", modify: (StyleBoxFlat style) =>
		{
			style.BgColor = Colours.NonogramTileBackground(mode: value, alternative: IsAlternative);
			style.SetBorderWidthAll(0);
			return style;
		});
	}
}
