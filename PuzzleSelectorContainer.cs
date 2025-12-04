using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class PuzzleSelectorContainer : PanelContainer
{
	public ColorRect Background { get; } = new ColorRect
	{
		Name = "Background",
		Color = Colors.Coral with { A = 0.5f },
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public ScrollContainer Scroll { get; } = new ScrollContainer()
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Puzzles { get; } = new VBoxContainer()
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public Labelled<VBoxContainer> PuzzlePacks { get; } = new()
	{
		Label = new RichTextLabel { Name = "Packs Title", FitContent = true, Text = "Puzzle Packs" }
		.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer()
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
	};

	private readonly List<PuzzleDisplay> _displays = [];
	private readonly List<PuzzlePackDisplay> _packDisplays = [];

	public override void _Ready()
	{
		this.Add(Background);

		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildExitingTree(Node node)
		{
			if (node is PuzzlePackDisplay pack && _packDisplays.Contains(pack))
			{
				_packDisplays.Remove(pack);
			}
		}
		void OnChildEnteredTree(Node node)
		{
			if (node is PuzzlePackDisplay pack)
			{
				_packDisplays.Add(pack);
			}
		}
	}

	public void ClearPacks()
	{
		PuzzlePacks.Value.Remove(true, _packDisplays);
	}
	public void Add(params IEnumerable<PuzzleData.Pack> packs)
	{
		foreach (PuzzleData.Pack pack in packs)
		{
			PuzzlePackDisplay packDisplay = new PuzzlePackDisplay(pack)
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			PuzzlePacks.Value.Add(packDisplay);
		}
	}


	public sealed partial class PuzzlePackDisplay : AspectRatioContainer
	{
		public VBoxContainer Puzzles { get; } = new VBoxContainer()
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		private readonly List<PuzzleDisplay> _displays = [];
		public PuzzlePackDisplay(PuzzleData.Pack pack)
		{
			foreach (PuzzleData puzzle in pack.Puzzles)
			{
				PuzzleDisplay display = new(puzzle);
				_displays.Add(display);
				Puzzles.Add(display);
			}
		}
	}
	public sealed partial class PuzzleDisplay : AspectRatioContainer
	{
		public Button Button { get; } = new() { Name = "Display Button" };

		public PuzzleDisplay(Display.Data data)
		{
			Name = data.Name + " Display";
			Button.Text = data.Name;
			Button.Pressed += () => Current.Puzzle = data;
		}

		public override void _Ready() => this.Add(Button);
	}
}

