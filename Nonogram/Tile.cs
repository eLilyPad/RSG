using Godot;

namespace RSG.Nonogram;

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
	public interface IConnectButton
	{
		void Pressed();
		void MouseEntered();
		void MouseExited();
	}
	public const string BlockText = "X", FillText = "O", EmptyText = " ";
	private const MouseButtonMask mask = MouseButtonMask.Left | MouseButtonMask.Right;

	public Button Button { get; } = new Button { Text = EmptyText, ButtonMask = mask }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AddAllFontThemeOverride(color: Colors.Transparent)
		.AddFontSizeOverride(value: 10)
		.OverrideStyle(name: "focus", modify: (StyleBox style) => new StyleBoxEmpty())
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
			style.SetBorderWidthAll(0);
			style.BgColor = Colors.Transparent;
			return style;
		});

	public IColours? Colours { private get; set; }

	public IConnectButton ButtonSignals
	{
		set
		{
			if (field is not null)
			{
				Button.ButtonDown -= field.Pressed;
				Button.MouseExited -= field.MouseExited;
				Button.MouseEntered -= field.MouseEntered;
			}
			Button.ButtonDown += value.Pressed;
			Button.MouseExited += value.MouseExited;
			Button.MouseEntered += value.MouseEntered;
			field = value;
		}
	}
	//[Export] public bool IsAlternative { get; set => Update(field = value); } = false;
	//[Export] public bool Locked { get; set => Button.SetLocked(locked: field = value); } = false;
	//[Export] public bool Hovering { get; private set => Button.SetHovering(hovering: field = value); } = false;
	//[Export] public TileMode Mode { get; set => Update(field = value); } = TileMode.Clear;

	public Tile()
	{
		Resized += () => Button.PivotOffset = Button.Size / 2;
	}
	public override void _Ready() => this.Add(Button);
	//private void Update<T>(T _) => this.SetColours(Mode, Colours);
}
