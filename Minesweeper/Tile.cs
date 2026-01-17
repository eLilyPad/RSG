using Godot;

namespace RSG.Minesweeper;

public sealed partial class Tile : PanelContainer
{
	public enum Mode { Bomb, Empty }

	internal interface IProvider
	{
		void OnActivate(Vector2I position, Tile tile) { }
		Mode GetType(Vector2I position) => Mode.Empty;
		//bool
	}
	internal sealed class Pool(Node parent, IColours colours) : NodePool<Vector2I, Tile>
	{
		public Vector2 TileSize { get; private set; } = Vector2.One;
		public IProvider? Provider { private get; set; }
		public override void Clear(IEnumerable<Vector2I> exceptions) => Clear(parent: _ => parent, exceptions);
		public void Update(int size)
		{
			IEnumerable<Vector2I> tileValues = (Vector2I.One * size).GridRange();
			bool firstTile = true;
			foreach (Vector2I position in tileValues)
			{
				Tile tile = GetOrCreate(position);
				tile.Type = Provider?.GetType(position) ?? Mode.Empty;

				if (firstTile)
				{
					TileSize = tile.Size;
					firstTile = false;
				}
			}
			Clear(exceptions: tileValues);
		}

		protected override Tile Create(Vector2I position)
		{
			Tile tile = new Tile { Name = $"Tile (X: {position.X}, Y: {position.Y})", Colours = colours }
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			parent.AddChild(tile);

			var button = tile.Button;

			button.ButtonDown += () => Provider?.OnActivate(position, tile);
			button
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
				.OverrideStyle(name: "focus", modify: (StyleBox style) => new StyleBoxEmpty());

			return tile;
		}
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = " ", ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);


	public required IColours Colours { private get; set; }
	[Export]
	public Mode Type
	{
		private get; set
		{
			Button.OverrideStyle(modify: (StyleBoxFlat style) =>
			{
				style.BgColor = Colours.MineSweeperBackground(mode: value, covered: Covered);
				return style;
			});

			field = value;
		}
	}
	[Export]
	public bool Covered
	{
		get; set
		{
			Button.OverrideStyle(modify: (StyleBoxFlat style) =>
			{
				style.BgColor = Colours.MineSweeperBackground(mode: Type, covered: value);
				return style;
			});
			Button.AddAllFontThemeOverride(Colors.Chocolate);
			field = value;
		}
	} = true;

	private Tile() { }
	public override void _Ready() => this.Add(Button);
}

