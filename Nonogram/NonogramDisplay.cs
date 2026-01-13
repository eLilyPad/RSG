using Godot;

namespace RSG.Nonogram;

public abstract partial class Display : AspectRatioContainer
{
	public sealed partial class Default : Display { }

	public const string BlockText = "X", FillText = "O", EmptyText = " ", EmptyHint = "0";
	public const MouseButton FillButton = MouseButton.Left, BlockButton = MouseButton.Right;
	public enum Type { Game, Display, Paint }

	public static TileMode PressedMode => Input.IsMouseButtonPressed(BlockButton) ? TileMode.Blocked
		: Input.IsMouseButtonPressed(FillButton) ? TileMode.Filled
		: TileMode.NULL;

	public MarginContainer Margin { get; } = new MarginContainer { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public Container Spacer { get; } = new PanelContainer { Name = "Spacer" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public TimerContainer Timer { get; } = new TimerContainer { Name = "Timer" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public GridContainer Grid { get; } = new GridContainer { Name = "MainContainer", Columns = 2 }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public VBoxContainer Rows { get; } = new VBoxContainer { Name = "RowHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public HBoxContainer Columns { get; } = new HBoxContainer { Name = "ColumnHints" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);


	public Container HintsParent(Side side) => side switch { Side.Row => Rows, Side.Column => Columns, _ => this };
	public override sealed void _Ready() => this.Add(
		Margin.Add(Grid.Add(Spacer.Add(Timer), Columns, Rows, TilesGrid))
	);
	public void ResetTheme()
	{
		const int marginValue = 100, spacerValue = 1;
		Grid.AddThemeConstantOverride("h_separation", 1);
		Grid.AddThemeConstantOverride("v_separation", 1);
		Rows.AddThemeConstantOverride("separation", spacerValue);
		Columns.AddThemeConstantOverride("separation", spacerValue);
		Margin.AddThemeConstantOverride("margin_top", marginValue);
		Margin.AddThemeConstantOverride("margin_bottom", marginValue / 2);
		TilesGrid.AddThemeConstantOverride("h_separation", 0);
		TilesGrid.AddThemeConstantOverride("v_separation", 0);
		TilesGrid.AddThemeConstantOverride("h_separation", spacerValue);
		TilesGrid.AddThemeConstantOverride("v_separation", spacerValue);
	}
}
