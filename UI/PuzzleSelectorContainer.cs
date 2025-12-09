using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class PuzzleSelector : PanelContainer
{
	public ColorRect Background { get; } = new ColorRect
	{
		Name = "Background",
		Color = Colors.DarkCyan,
	}
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	//public VBoxContainer Puzzles { get; } = new VBoxContainer()
	//	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public Labelled<VBoxContainer> Puzzles { get; } = new Labelled<VBoxContainer>()
	{
		Name = "Puzzles Container",
		Vertical = true,
		Label = new RichTextLabel { Name = "PuzzlesTitle", FitContent = true, Text = "Puzzles" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	private readonly List<PackDisplay> _packDisplays = [];

	public override void _Ready()
	{
		this.Add(Background, Scroll.Add(Puzzles));

		ChildEnteredTree += OnChildEnteredTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildExitingTree(Node node)
		{
			if (node is PackDisplay pack && _packDisplays.Contains(pack))
			{
				_packDisplays.Remove(pack);
			}
		}
		void OnChildEnteredTree(Node node)
		{
			if (node is PackDisplay pack)
			{
				_packDisplays.Add(pack);
			}
		}
	}

	public void ClearPacks()
	{
		Puzzles.Value.Remove(true, _packDisplays);
	}

	public sealed partial class PackDisplay : AspectRatioContainer
	{
		public Labelled<VBoxContainer> Puzzles { get; } = new Labelled<VBoxContainer>()
		{
			Label = new RichTextLabel { Name = "Label", FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = new VBoxContainer { Name = "Puzzles Container" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize),
			Vertical = true
		}
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		private readonly List<PuzzleDisplay> _displays = [];
		public PackDisplay()
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
	public sealed partial class PuzzleDisplay : BoxContainer
	{
		public required ColorRect Background { get; init; }
		public required Button Button { get; init; }
		public override void _Ready() => this.Add(Background, Button);
	}
}

