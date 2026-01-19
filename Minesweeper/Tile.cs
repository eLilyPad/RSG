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
				Mode mode = tile.Type = Provider?.GetType(position) ?? Mode.Empty;
				tile.Covered = true;
				int bombsAround = 0;

				switch (mode)
				{
					case Mode.Empty:
						foreach (Vector2I around in tileValues.PointsAround(position))
						{
							if (Provider?.GetType(position: around) is Mode.Bomb) bombsAround++;
						}
						break;
					default: break;
				}

				tile.Button.Text = bombsAround is 0 ? string.Empty : bombsAround.ToString();
				tile.Button.AddThemeFontSizeOverride("normal", (int)tile.Size.LengthSquared() * 3);

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

			button.AddThemeFontSizeOverride("normal", 40);
			button.ButtonDown += () => Provider?.OnActivate(position, tile);

			return tile;
		}
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;
	private static MinesweeperTextures Textures => field ??= Core.MinesweeperTexturesPath.LoadOrCreateResource<MinesweeperTextures>();

	public TextureRect Image { get; } = new TextureRect
	{
		Name = "Image",
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		MouseFilter = MouseFilterEnum.Ignore
	}
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	public Button Button { get; } = new Button { Text = " ", ButtonMask = mask, ExpandIcon = false }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
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
	//public 

	public required IColours Colours { private get; set; }
	[Export]
	public Mode Type
	{
		get; set
		{
			Button.OverrideStyle(modify: (StyleBoxFlat style) =>
			{
				style.BgColor = Colours.MineSweeperBackground(mode: value, covered: Covered);
				return style;
			});
			if (value is Mode.Bomb && !Covered) Image.Texture = Textures.Bomb;

			field = value;
		}
	}
	[Export]
	public bool Flagged
	{
		get; set
		{
			if (value && !Covered) return;
			Image.Texture = value ? Textures.Flag : null;
			field = value;
		}
	} = false;
	[Export]
	public bool Covered
	{
		get; set
		{
			Button.OverrideStyle(modify: (StyleBoxFlat style) =>
			{
				style.BgColor = Colours.MineSweeperBackground(mode: Type, covered: value);
				return style;
			})
			.AddAllFontThemeOverride(value ? Colors.Transparent : Colors.Chocolate);

			Image.Texture = Type switch
			{
				Mode.Bomb when !value => Textures.Bomb,
				_ => Image.Texture
			};

			field = value;
		}
	} = true;

	private Tile() { }
	public override void _Ready() => this.Add(Button, Image);
}

