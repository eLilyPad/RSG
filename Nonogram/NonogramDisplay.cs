using Godot;


namespace RSG.Nonogram;

using static Display;

public interface IDisplayPools<TTiles, THints>
{
	TTiles Tiles { get; }
	THints Hints { get; }
}

public interface IDisplayHints
{
	VBoxContainer Rows { get; }
	HBoxContainer Columns { get; }
	Container HintsParent(Side side);
}
public abstract partial class Display : AspectRatioContainer, IDisplayHints
{
	internal sealed partial class Default : Display { }
	public sealed partial class DisplaySpacer : PanelContainer, TimerContainer.IHave
	{
		public TimerContainer Timer { get; } = new TimerContainer { Name = "Timer" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	}
	public const MouseButton FillButton = MouseButton.Left, BlockButton = MouseButton.Right;

	public static TileMode PressedMode => Input.IsMouseButtonPressed(BlockButton) ? TileMode.Blocked
		: Input.IsMouseButtonPressed(FillButton) ? TileMode.Filled
		: TileMode.NULL;

	public MarginContainer Margin { get; } = new MarginContainer { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public DisplaySpacer Spacer { get; } = new DisplaySpacer { Name = "Spacer" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public TimerContainer Timer { get; } = new TimerContainer { Name = "Timer" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer Grid { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

	private Display() { }
	public override sealed void _Ready() => this.Add(
		Margin.Add(Grid.Add(Spacer.Add(Timer), Columns, Rows, TilesGrid))
	);
	public Container HintsParent(Side side) => side switch
	{
		Side.Row => Rows,
		_ => Columns
	};
}
