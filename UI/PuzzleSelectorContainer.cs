using Godot;

namespace RSG.Nonogram;

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

	public override void _Ready() => this
		.Add(Background, Scroll.Add(Puzzles))
		.LinkToParent(_packDisplays);

	public void ClearPacks()
	{
		Puzzles.Value.RemoveChildren(true);
	}
	public void Fill(UI.MainMenu menu, IEnumerable<PuzzleData.Pack> packs)
	{
		foreach (PuzzleData.Pack pack in packs)
		{
			PackDisplay display = new PackDisplay { Name = pack.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			display.Puzzles.Label.Text = pack.Name;
			foreach (PuzzleData puzzle in pack.Puzzles)
			{
				PuzzleDisplay puzzleDisplay = new()
				{
					Name = puzzle.Name + " Display",
					Button = new() { Name = puzzle.Name + " Button", Text = puzzle.Name },
					Background = new ColorRect { Name = "Background", Color = Colors.Black }
						.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
				};
				puzzleDisplay.Button.Pressed += Pressed;
				display.Puzzles.Value.Add(puzzleDisplay);

				void Pressed()
				{
					PuzzleManager.Current.Puzzle = puzzle;
					Hide();
					menu.Hide();
				}
			}
			Puzzles.Value.Add(display);
		}
	}
	public void Fill(UI.MainMenu menu, IEnumerable<SaveData> saves)
	{
		PackDisplay saved = new PackDisplay { Name = "SavedDisplay" }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		saved.Puzzles.Label.Text = "Saved";
		Puzzles.Value.Add(saved);
		foreach (SaveData save in saves)
		{
			Color statusColor = save.IsComplete ? Colors.Green : Colors.Black;
			PuzzleDisplay puzzleDisplay = new PuzzleDisplay()
			{
				Name = save.Name + " Display",
				Button = new() { Name = save.Name + " Button", Text = save.Name },
				Background = new ColorRect { Name = "Background", Color = statusColor }
					.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			puzzleDisplay.Button.Pressed += Pressed;
			saved.Puzzles.Value.Add(puzzleDisplay);

			void Pressed()
			{
				PuzzleManager.Current.Puzzle = save;
				Hide();
				menu.Hide();
			}
		}
	}

	sealed partial class PackDisplay : AspectRatioContainer
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
		internal PackDisplay() { }
		public override void _Ready() => this
			.Add(Puzzles)
			.LinkToParent(_displays);
	}
	sealed partial class PuzzleDisplay : BoxContainer
	{
		public required ColorRect Background { get; init; }
		public required Button Button { get; init; }
		public override void _Ready() => this.Add(Background, Button);
	}
}

