using Godot;

namespace RSG.Nonogram;

using static Display;

public sealed partial class Tile : PanelContainer
{
	public sealed class Locker
	{
		public required List<Func<Vector2I, bool>> Rules { private get; init; }
		public bool ShouldLock(Vector2I position) => Rules.Any(rule => rule(position));
	}
	internal interface IProvider
	{
		Node Parent();
		void OnActivate(Vector2I position, Tile tile) { }
		TileMode State(Vector2I position) => TileMode.Clear;
	}
	internal sealed class Pool(IProvider Provider, IColours Colours) : NodePool<Vector2I, Tile>
	{
		public required Locker LockRules { get; init; }
		public Vector2 TileSize { get; private set; } = Vector2.One;


		public void Update(int size)
		{
			IEnumerable<Vector2I> tileValues = (Vector2I.One * size).GridRange();
			bool firstTile = true;
			foreach (Vector2I position in tileValues)
			{
				Tile tile = GetOrCreate(position);

				tile.Mode = Provider.State(position);
				tile.Locked = LockRules.ShouldLock(position);

				if (firstTile)
				{
					TileSize = tile.Size;
					firstTile = false;
				}
			}

			Clear(exceptions: tileValues);
		}
		public override void Clear(IEnumerable<Vector2I> exceptions) => Clear(_ => Provider.Parent(), exceptions);
		protected override Tile Create(Vector2I position)
		{
			const int chunkSize = 5;
			Tile tile = new Tile
			{
				Name = $"Tile (X: {position.X}, Y: {position.Y})",
				IsAlternative = (position.X / chunkSize + position.Y / chunkSize) % 2 == 0,
				Colours = Colours,
				Mode = TileMode.Clear,
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			Provider.Parent().AddChild(tile);

			tile.Resized += () => tile.Button.PivotOffset = tile.Button.Size / 2;
			tile.Button.ButtonDown += () => Provider.OnActivate(position, tile);
			tile.Button.MouseExited += () => HoverTile(false);
			tile.Button.MouseEntered += () =>
			{
				Provider.OnActivate(position, tile);
				HoverTile(true);
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

			void HoverTile(bool hovering)
			{
				var tiles = _nodes.AllInLines(position);
				foreach ((Vector2I _, Tile tile) in tiles) tile.Hovering = hovering;
			}
		}
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

	public bool IsAlternative { get; private init; } = false;
	public required IColours Colours { private get; set; }
	[Export] public bool Locked { get; set => ChangeLocked(field = value); } = false;
	[Export] public bool Hovering { get; set => ChangeHovering(field = value); } = false;
	[Export] public TileMode Mode { get; set => ChangeMode(field = value); } = TileMode.NULL;

	private Tile() { }
	public override void _Ready() => this.Add(Button);

	private void ChangeHovering(bool value) => Button.Scale = Vector2.One * (value ? .9f : 1);
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
