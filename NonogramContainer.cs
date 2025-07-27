using Godot;
using static Godot.Control;

namespace RSG;

using UI.Nonogram;

public partial class NonogramContainer : VBoxContainer
{
	public static PuzzleData.Codifier CodeSaver { get; } = new();
	public static PuzzleData.PuzzleSaver JsonSaver { get; } = new();

	public PuzzleData Puzzle
	{
		get; set
		{
			Painter.Puzzle = Game.Puzzle = field = value;
			JsonSaver.Name = field.Name;
		}
	} = new();
	public PaintContainer Painter { get; } = new() { Name = "Painter", PuzzleSize = PuzzleData.DefaultSize };
	public GameContainer Game { get; } = new() { Name = "Game", PuzzleSize = PuzzleData.DefaultSize };
	public LoadMenu LoadingMenu { get; } = new() { Name = "LoadMenu" };
	public TabContainer Displays { get; } = new TabContainer { Name = "Tabs", TabsVisible = true }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ToolBarContainer ToolBar { get; } = new ToolBarContainer { Name = "Toolbar", SizeFlagsStretchRatio = 0.05f }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(0, 0, 0) }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready()
	{
		this.Add(ToolBar, Displays.Add(Game, Painter), LoadingMenu)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		ToolBar.Reset.Pressed += () => Puzzle = PuzzleData.Empty;
		ToolBar.Tools.Painters.NameInput.TextChanged += value => JsonSaver.Name = Puzzle.Name = value;
		ToolBar.Tools.Painters.SaveAs.Pressed += () =>
		{
			if (Painter.Puzzle.TryPickT0(value: out PuzzleData puzzle, remainder: out _)) JsonSaver.Save(puzzle);
		};
		ToolBar.Tools.Games.CheckProgress.Pressed += () =>
		{
			ToolBar.Tools.Games.ProgressReport.Text = Puzzle.Matches(Game) ? "Correct" : "Wrong";
		};
		LoadingMenu.Load.Pressed += () =>
		{
			JsonSaver.Name = LoadingMenu.Puzzles.GetSelectedItem().Match(name => name, _ => JsonSaver.Name);
			JsonSaver.Load().Switch(puzzle => Puzzle = puzzle, _ => { });
		};
		LoadingMenu.Puzzles.ItemSelected += index =>
		{
			JsonSaver.Name = LoadingMenu.Puzzles.GetItemText((int)index);
			JsonSaver.Load().Switch(puzzle => Puzzle = puzzle, _ => { });
		};
		Game.VisibilityChanged += () =>
		{
			if (Game.IsVisibleInTree())
			{
				ToolBar.Tools.Games.AddTools();
				Puzzle.UpdateDisplay(Game);
			}
			else ToolBar.Tools.Games.RemoveTools();
		};
		Painter.VisibilityChanged += () =>
		{
			if (Painter.IsVisibleInTree()) ToolBar.Tools.Painters.AddTools();
			else ToolBar.Tools.Painters.RemoveTools();
		};
	}

	public partial class Menu : MenuBar
	{

	}
}


public partial class LoadMenu : PopupMenu
{
	public OptionButton Puzzles { get; } = new OptionButton()
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.AnchorsAndOffsetsPreset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Button Load { get; } = new() { Name = "Load", Text = "Load" };
	public override void _Ready() => this.Add(Puzzles);
}
public partial class ToolBarContainer : HBoxContainer
{
	public (ToolBar.Game Games, ToolBar.Paint Painters) Tools { get; }
	public Button OpenLoader { get; } = new() { Name = "OpenLoad", Text = "Load" };
	public Button Reset { get; } = new() { Name = "ResetTiles", Text = "Reset Tiles" };
	public ToolBarContainer() => Tools = (new() { Container = this }, new() { Container = this });
	public override void _Ready() => this.Add(OpenLoader, Reset);
}
public sealed partial class PaintContainer : PuzzleDisplay
{
	public override void OnTilePressed(Button button, Vector2I position)
	{
		base.OnTilePressed(button, position);
		UpdateHintsAt(position);
	}
}
public sealed partial class GameContainer : PuzzleDisplay;