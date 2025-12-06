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
	public List<PuzzleContainer> Packs { private get; init; } = [];

	public override void _Ready()
	{
		this.Add(Loader.Add(PuzzleLoader, CodeLoader), Saver)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildEnteredTree(Node node)
		{
			switch (node)
			{
				case Button button when PuzzleLoader.Control.Saved.HasChild(button):
					Puzzles.Add(button);
					break;
				case PuzzleContainer puzzle:
					Packs.Add(puzzle);
					break;
			}
		}
		void OnChildExitingTree(Node node)
		{
			switch (node)
			{
				case Button button when PuzzleLoader.Control.Saved.HasChild(button):
					Puzzles.Remove(button);
					break;
				case PuzzleContainer puzzle:
					Packs.Remove(puzzle);
					break;
			}
		}
	}
	public void AddPuzzles(params IEnumerable<PuzzleData.Pack> packs)
	{
		foreach (PuzzleData.Pack pack in packs)
		{
			PuzzleContainer container = new PuzzleContainer
			{
				Name = "Pack Container",
				Alignment = AlignmentMode.Begin,
				Title = new RichTextLabel { Name = "PuzzlePackTitle", Text = pack.Name, FitContent = true }
					.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize),
				Container = new VBoxContainer { Name = "PuzzlesPackContainer", Alignment = AlignmentMode.End }
					.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
			}
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);

			foreach (var puzzle in pack.Puzzles)
			{
				Button button = new() { Text = puzzle.Name };
				button.Pressed += () => Current.Puzzle = puzzle;
				container.Container.Add(button);
			}

			PuzzleLoader.Control.Add(container);
		}
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
		public required RichTextLabel Title { get; init; }
		public required VBoxContainer Container { get; init; }
		public override void _Ready() => this.Add(Title, Container);
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
