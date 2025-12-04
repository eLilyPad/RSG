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
	public List<PuzzleContainer> Packs { private get; init; } = [];

	public override void _Ready()
	{
		this.Add(Loader.Add(PuzzleLoader, CodeLoader), Saver)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildEnteredTree(Node node)
		{
			if (node is Button button && PuzzleLoader.Control.Saved.HasChild(button))
			{
				Puzzles.Add(button);
			}
		}
		void OnChildExitingTree(Node node)
		{
			if (node is Button button && PuzzleLoader.Control.Saved.HasChild(button))
			{
				Puzzles.Remove(button);
			}
		}
	}
	public void AddPuzzles(PuzzleData.Pack pack)
	{
		PuzzleContainer container = new PuzzleContainer
		{
			Name = "Pack Container",
			Alignment = AlignmentMode.Begin,
			Title = new RichTextLabel { Name = "PuzzlePackTitle", Text = pack.Name, FitContent = true },
			Container = new VBoxContainer { Name = "PuzzlesPackContainer", Alignment = AlignmentMode.End }
		}
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);

		foreach (var puzzle in pack.Puzzles)
		{
			Button button = new() { Text = puzzle.Name };
			button.Pressed += () => Current.Puzzle = puzzle;
			container.Container.Add(button);
		}

		Packs.Add(container);
		PuzzleLoader.Control.Add(container);
	}
	public sealed partial class PuzzleLoaderContainer : VBoxContainer
	{
		public PuzzleContainer Saved = new()
		{
			Name = "Saves Container",
			Title = new RichTextLabel { Name = "Save Puzzle Title", Text = "Saved Puzzles", FitContent = true },
			Container = new VBoxContainer { Name = "Saved Puzzles Container" }
		};
		public override void _Ready() => this.Add(Saved);
	}
	public sealed partial class PuzzleContainer : VBoxContainer
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
		public PuzzleContainer Fill<T>(IList<Button> buttons, IEnumerable<T> puzzles) where T : Display.Data
		{
			Container.Remove(free: true, buttons);
			foreach (var puzzle in puzzles)
			{
				Button button = new() { Text = puzzle.Name };
				button.Pressed += () => Current.Puzzle = puzzle;
				Container.Add(button);
			}
			return this;
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
