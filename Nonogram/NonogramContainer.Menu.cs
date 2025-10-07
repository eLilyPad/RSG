using Godot;
using static Godot.BoxContainer;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class Menu : MenuBar
{
	public Popup<CodeLoaderContainer> CodeLoader { get; } = new()
	{
		Name = "Code Loader",
		Control = new CodeLoaderContainer { Name = "Code Loader Container" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill),
	};
	public Popup<PuzzleLoaderContainer> PuzzleLoader { get; } = new()
	{
		Name = "Puzzle Loader",
		Control = new PuzzleLoaderContainer { Name = "Puzzle Loader Container" }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill),
	};
	public PopupMenu Loader { get; } = new PopupMenu { Name = "Load" };
	public PopupMenu Saver { get; } = new PopupMenu { Name = "Save" };

	public List<Button> Puzzles { private get; init; } = [];
	public List<Button> SavedPuzzles { private get; init; } = [];
	public List<PuzzleLoaderContainer.ButtonsContainer> Packs { private get; init; } = [];

	public override void _Ready() => this
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.Add(Loader.Add(PuzzleLoader, CodeLoader), Saver);
	public void LoadSavedPuzzles()
	{
		IList<SaveData> puzzles = GetSavedPuzzles();
		PuzzleLoader.Control.Saved.FillPuzzles(SavedPuzzles, puzzles);
	}
	public void AddPuzzles(PuzzleData.Pack pack)
	{
		PuzzleLoaderContainer.ButtonsContainer container = new PuzzleLoaderContainer.ButtonsContainer
		{
			Name = "Pack Container",
			Alignment = AlignmentMode.End,
			Title = new RichTextLabel { Name = "PuzzlePackTitle", Text = pack.Name, FitContent = true },
			Container = new VBoxContainer { Name = "PuzzlesPackContainer" }
		}
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
		.FillPuzzles(Puzzles, pack.Puzzles);
		Packs.Add(container);
		PuzzleLoader.Control.Add(container);
	}
	public sealed partial class PuzzleLoaderContainer : VBoxContainer
	{
		public ButtonsContainer Saved = new()
		{
			Name = "Saves Container",
			Title = new RichTextLabel { Name = "Save Puzzle Title", Text = "Saved Puzzles", FitContent = true },
			Container = new VBoxContainer { Name = "Saved Puzzles Container" }
		};
		public override void _Ready() => this.Add(Saved);
		public sealed partial class ButtonsContainer : VBoxContainer
		{
			public required RichTextLabel Title
			{
				get; init => field = value.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize);
			}
			public required VBoxContainer Container
			{
				get; init => field = value.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			}
			public override void _Ready() => this.Add(Title, Container);
			public ButtonsContainer FillPuzzles(IList<Button> buttons, IEnumerable<SaveData> puzzles)
			{
				foreach (Button puzzle in buttons)
				{
					if (!IsInstanceValid(puzzle)) { continue; }
					puzzle.QueueFree();
				}
				foreach (SaveData save in puzzles)
				{
					Button button = new() { Text = save.Name };
					button.Pressed += () => Current.Puzzle = save;
					buttons.Add(button);
					Container.Add(button);
				}
				return this;
			}
			public ButtonsContainer FillPuzzles(IList<Button> buttons, IEnumerable<PuzzleData> puzzles)
			{
				foreach (Button puzzle in buttons) { this.Remove(free: true, puzzle); }
				foreach (PuzzleData puzzle in puzzles)
				{
					Button button = new() { Text = puzzle.Name };
					button.Pressed += () => Current.Puzzle = puzzle;
					buttons.Add(button);
					Container.Add(button);
				}
				return this;

			}
		}
	}
	public partial class CodeLoaderContainer : VBoxContainer
	{
		public const string InputPlaceholder = "Enter Nonogram Code Here ...";
		public LineEdit Input { get; } = new LineEdit { Name = "CodeInput", PlaceholderText = InputPlaceholder }
		.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize);
		public RichTextLabel Validation { get; } = new RichTextLabel { Name = "Validation", FitContent = true, Text = "Empty" }
		.Preset(LayoutPreset.BottomLeft, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(Input, Validation);
	}
}
