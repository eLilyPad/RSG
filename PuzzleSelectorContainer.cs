using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class PuzzleSelectorContainer : PanelContainer
{
	public ColorRect Background { get; } = new ColorRect
	{
		Name = "Background",
		Color = Colors.AliceBlue with { A = 0.5f },
	}
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	//public VBoxContainer Puzzles { get; } = new VBoxContainer()
	//	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public Labelled<VBoxContainer> PuzzlePacks { get; } = new Labelled<VBoxContainer>()
	{
		Label = new RichTextLabel { Name = "Packs Title", FitContent = true, Text = "Puzzle Packs" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer()
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
	}
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
	private readonly List<PuzzlePackDisplay> _packDisplays = [];

	public override void _Ready()
	{
		this.Add(Background, Scroll.Add(PuzzlePacks));

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

	public sealed partial class PuzzlePackDisplay : AspectRatioContainer
	{
		public Labelled<VBoxContainer> Puzzles { get; } = new Labelled<VBoxContainer>()
		{
			Label = new RichTextLabel { Name = "Label", FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = new VBoxContainer { Name = "Puzzles Container" }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
		}
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		private readonly List<PuzzleDisplay> _displays = [];
		public PuzzlePackDisplay()
		{
			ChildEnteredTree += OnChildEnteredTree;
			ChildExitingTree += OnChildExitingTree;

			void OnChildExitingTree(Node node)
			{
				if (node is PuzzleDisplay display && _displays.Contains(display))
				{
					_displays.Remove(display);
				}
			}
			void OnChildEnteredTree(Node node)
			{
				if (node is PuzzleDisplay display)
				{
					_displays.Add(display);
				}
			}
		}
		public override void _Ready() => this.Add(Puzzles);
	}
	public sealed partial class PuzzleDisplay : AspectRatioContainer
	{
		public Button Button { get; } = new() { Name = "Display Button" };
		public override void _Ready() => this.Add(Button);
	}
}

