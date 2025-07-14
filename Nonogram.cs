using Godot;

namespace RSG;

using static UI.Nonogram.Display;
using UI.Nonogram;

public partial class Nonogram : VBoxContainer
{
	public partial class LoadMenu : PopupMenu
	{
		public OptionButton Puzzles { get; } = new OptionButton()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public Button Load { get; } = new() { Name = "Load", Text = "Load" };
		public override void _Ready() => this.Add(Puzzles, Load);
	}
	public partial class ToolBarContainer : HBoxContainer
	{
		public Button OpenLoader { get; } = new() { Name = "OpenLoad", Text = "Load" };
		public Button Reset { get; } = new() { Name = "ResetTiles", Text = "Reset Tiles" };
		public override void _Ready() => this.Add(OpenLoader, Reset);
	}
	public sealed class PaintToolBar : ToolBar
	{
		public Button SaveAs { get; } = new() { Name = "Save", Text = "Save As" };
		public Button SaveAsCode { get; } = new() { Name = "SaveCode", Text = "As Code" };
		public LineEdit NameInput { get; } = new LineEdit
		{
			Name = "NameInput",
			TooltipText = "New Puzzle"
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public override void AddTools() => Container.Add(SaveAs, NameInput, SaveAsCode);
		public override void RemoveTools() => Container.Remove(SaveAs, NameInput, SaveAsCode);
	}
	public sealed class GameToolBar : ToolBar
	{
		public Button CheckProgress { get; } = new() { Name = "CheckProgress", Text = "Check" };
		public RichTextLabel ProgressReport { get; } = new RichTextLabel
		{
			Name = "ProgressReport",
			SizeFlagsStretchRatio = 0.05f
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

		public override void AddTools() => Container.Add(CheckProgress, ProgressReport);
		public override void RemoveTools() => Container.Remove(CheckProgress, ProgressReport);
	}

	public sealed partial class PaintContainer : DataDisplay<PuzzleData>
	{
		public required GameContainer Game { get; init; }
		public override void OnTilePressed(Button button, Vector2I position)
		{
			base.OnTilePressed(button, position);
			UpdateHintsAt(position);
			Game.UpdateHintsAt(position);
			GD.Print(CodeSaver.Encode(Puzzle));
		}
		public override void Reset()
		{
			base.Reset();
			foreach (var (_, label) in Labels) { label.Text = EmptyHint; }
		}
	}
	public sealed partial class GameContainer : DataDisplay<PuzzleData>;

	public static PuzzleData.SaveCode CodeSaver { get; } = new();
	public static PuzzleData.PuzzleSaver JsonSaver { get; } = new();

	public PuzzleData Puzzle { get; set => JsonSaver.Name = (Painter.Puzzle = Game.Puzzle = field = value).Name; } = new();
	public PaintContainer Painter => field ??= new() { Name = "Painter", Game = Game };
	public GameContainer Game { get; } = new() { Name = "Game" };

	public LoadMenu LoadingMenu { get; } = new() { Name = "LoadMenu" };
	public TabContainer Displays { get; } = new TabContainer { Name = "Tabs", TabsVisible = true }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ToolBarContainer ToolBar { get; } = new ToolBarContainer { Name = "Toolbar", SizeFlagsStretchRatio = 0.05f }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(0, 0, 0) }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public GameToolBar GamesToolBar => field ??= new() { Container = ToolBar };
	public PaintToolBar PaintersToolBar => field ??= new() { Container = ToolBar };

	public override void _Ready()
	{
		this.Add(ToolBar, Displays.Add(Game, Painter))
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		Painter.PuzzleSize = Game.PuzzleSize = 10;

		ToolBar.Reset.Pressed += () =>
		{
			if (Displays.GetCurrentTabControl() is not Display display) return;
			display.Reset();
			if (display is PaintContainer) Puzzle.Reset();
		};
		LoadingMenu.Load.Pressed += () =>
		{
			bool hasName = LoadingMenu.Puzzles.GetItemCount() == 0 && LoadingMenu.Puzzles.Selected == -1;
			JsonSaver.Name = hasName ? LoadingMenu.Puzzles.GetItemText(LoadingMenu.Puzzles.Selected) : "";
			JsonSaver.Load().Switch(puzzle => Puzzle = puzzle, _ => { });
		};
		LoadingMenu.Puzzles.ItemSelected += index =>
		{
			JsonSaver.Name = LoadingMenu.Puzzles.GetItemText((int)index);
			JsonSaver.Load().Switch(puzzle => Puzzle = puzzle, _ => { });
		};
		PaintersToolBar.NameInput.TextChanged += value => JsonSaver.Name = Puzzle.Name = value;
		PaintersToolBar.SaveAs.Pressed += () => JsonSaver.Save(Painter.Puzzle);
		GamesToolBar.CheckProgress.Pressed += () => GamesToolBar.ProgressReport.Text = Puzzle.Matches(Game)
			? "Correct"
			: "Wrong";
		Game.VisibilityChanged += ChangeToolBar(GamesToolBar, Game);
		Painter.VisibilityChanged += ChangeToolBar(PaintersToolBar, Painter);

		static Action ChangeToolBar(ToolBar tools, Container parent) => () =>
		{
			if (parent.IsVisibleInTree()) tools.AddTools(); else tools.RemoveTools();
		};
	}
}