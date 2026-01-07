using Godot;

namespace RSG.Nonogram;

using static Display;

public sealed partial class Tile : PanelContainer
{
	internal interface IProvider
	{
		Node Parent();
		void OnActivate(Vector2I position, Tile tile);
	}
	internal sealed class Display(IProvider Provider, IColours Colours) : NodePool<Vector2I, Tile>
	{
		public override void Clear(IEnumerable<Vector2I> exceptions) => Clear(_ => Provider.Parent(), exceptions);
		protected override Tile Create(Vector2I position)
		{
			Tile tile = new(position, colours: Colours, provider: Provider, lineTiles: LineTiles) { };
			Provider.Parent().AddChild(tile);
			return tile;
			IEnumerable<Tile> LineTiles() => _nodes.AllInLines(position).Select(p => p.Value);
		}
	}
	private const string themeName = "normal";
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

	public bool IsAlternative { get; private init; } = false;
	public bool Hovering { get; set => Button.Scale = Vector2.One * ((field = value) ? .9f : 1); } = false;
	[Export]
	public TileMode Mode
	{
		get; set
		{
			field = value;
			Style.BgColor = Background;
			Button.AddThemeStyleboxOverride(themeName, Style);
		}
	} = TileMode.NULL;

	public IColours Colours { private get; init; }

	private StyleBoxFlat Style { get; init; } = new();
	private Color Background => Mode switch
	{
		TileMode.Filled when IsAlternative => Colours.NonogramTileBackgroundFilled,
		TileMode.Filled => Colours.NonogramTileBackgroundFilled.Darkened(.2f),
		TileMode.Blocked when IsAlternative => Colours.NonogramTileBackground1.Darkened(.4f),
		TileMode.Blocked => Colours.NonogramTileBackground2.Darkened(.4f),
		_ when IsAlternative => Colours.NonogramTileBackground1,
		_ => Colours.NonogramTileBackground2
	};

	public override void _Ready() => this.Add(Button);
	private Tile(Vector2I position, IColours colours, IProvider provider, Func<IEnumerable<Tile>> lineTiles)
	{
		const int chunkSize = 5;

		Name = $"Tile (X: {position.X}, Y: {position.Y})";
		Colours = colours;
		IsAlternative = (position.X / chunkSize + position.Y / chunkSize) % 2 == 0;
		Resized += () => Button.PivotOffset = Button.Size / 2;
		Button.ButtonDown += () => provider.OnActivate(position, tile: this);
		Button.MouseEntered += () => provider.OnActivate(position, tile: this);
		Button.MouseExited += () => HoverTile(false);
		Button.MouseEntered += () => HoverTile(true);

		if (Button.GetThemeStylebox(themeName).Duplicate() is StyleBoxFlat style)
		{
			Style = style;
			style.CornerDetail = 1;
			style.SetCornerRadiusAll(0);
			style.BgColor = Background;
			Button.AddThemeStyleboxOverride(themeName, style);
		}
		Button.AddAllFontThemeOverride(Colors.Transparent);
		Button.AddThemeFontSizeOverride("font_size", 10);

		this.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

		void HoverTile(bool hovering)
		{
			foreach (Tile tile in lineTiles()) tile.Hovering = hovering;
		}
	}
}
