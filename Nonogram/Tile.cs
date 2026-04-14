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
		void OnActivate(Vector2I position, Tile tile) { }
		TileMode State(Vector2I position) => TileMode.Clear;
	}
	public interface ILockPool
	{
		bool ShouldLock(Vector2I position);
		void UnLockAll();
		bool TryLock(Vector2I position);
		bool TryLock(Vector2I position, Type type)
		{
			if (type is not Type.Game) return false;
			return TryLock(position);
		}
	}
	public interface IRefresh { void Refresh(); }
	public interface IPool : ILockPool, IRefresh
	{
		Vector2 TileSize { get; }
		void Resize(int value);
	}
	internal sealed class Pool
	{
		public const int ChunkSize = 5;
	}
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;
	public static Tile Create(Vector2I position)
	{
		return new Tile
		{
			Name = $"Tile (X: {position.X}, Y: {position.Y})",
			IsAlternative = (position.X / Pool.ChunkSize + position.Y / Pool.ChunkSize) % 2 == 0,
			Colours = Core.Colours,
			Mode = TileMode.Clear,
		}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	}

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
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
		.OverrideStyle(name: "focus", modify: (StyleBox style) => new StyleBoxEmpty())
		.AddAllFontThemeOverride(Colors.Transparent);

	public bool IsAlternative { get; init; } = false;
	public required IColours Colours { private get; set; }
	[Export] public bool Locked { get; set => ChangeLocked(field = value); } = false;
	[Export] public bool Hovering { get; set => ChangeHovering(field = value); } = false;
	[Export] public TileMode Mode { get; set => ChangeMode(field = value); } = TileMode.Clear;

	private Tile() => Resized += () => Button.PivotOffset = Button.Size / 2;
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
