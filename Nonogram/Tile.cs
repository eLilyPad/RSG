using Godot;

namespace RSG.Nonogram;

using static Display;

sealed class Tiles(Tiles.IProvider Provider, IColours Colours) : NodePool<Vector2I, Tile>
{
	internal interface IProvider
	{
		Node Parent();
		string Text(Vector2I position);
		void Activate(Vector2I position, Tile tile);
	}
	public override Tile GetOrCreate(Vector2I position) => _nodes.GetOrCreate(key: position, create: Create);
	public override void Clear(IEnumerable<Vector2I> exceptions) => Clear(_ => Provider.Parent(), exceptions);
	public void ApplyText(Vector2I position, Tile tile, TileMode? input = null)
	{
		tile.Button.Text = Provider.Text(position);
		tile.Button.StyleTileBackground(position, colours: Colours, mode: input);
	}
	private Tile Create(Vector2I position)
	{
		Tile tile = new Tile { Name = $"Tile (X: {position.X}, Y: {position.Y})" }
			.SizeFlags(Control.SizeFlags.ExpandFill, Control.SizeFlags.ExpandFill);
		Provider.Parent().AddChild(tile);
		if (tile.Button.GetThemeStylebox("normal").Duplicate() is StyleBoxFlat style)
		{
			style.CornerDetail = 1;
			style.SetCornerRadiusAll(0);
			tile.Button.StyleTileBackground(position, Colours, style);
		}
		tile.Button.AddAllFontThemeOverride(Colors.Transparent);
		tile.Button.AddThemeFontSizeOverride("font_size", 10);
		tile.Resized += () => tile.Button.PivotOffset = tile.Button.Size / 2;
		tile.Button.ButtonDown += () => Provider.Activate(position, tile);
		tile.Button.MouseExited += () => HoverTile(position, tile, false);
		tile.Button.MouseEntered += () => HoverTile(position, tile, true);
		return tile;
	}

	private void HoverTile(Vector2I position, Tile tile, bool hovering)
	{
		Vector2 scale;
		if (hovering)
		{
			Provider.Activate(position, tile);
			scale = Vector2.One * .9f;
		}
		else scale = Vector2.One * 1;

		foreach ((Vector2I _, Tile other) in _nodes.AllInLines(position))
		{
			other.Button.Scale = scale;
		}
	}
}
public sealed partial class Tile : PanelContainer
{
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;
	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

	public override void _Ready() => this.Add(Button);
}
