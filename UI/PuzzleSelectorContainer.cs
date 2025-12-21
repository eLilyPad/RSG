using Godot;

namespace RSG.Nonogram;

public sealed partial class PuzzleSelector : PanelContainer
{
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkCyan }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
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

	public override void _Ready() => this.Add(Background, Scroll.Add(Puzzles));

	public void ClearPacks()
	{
		foreach (PackDisplay pack in _packDisplays)
		{
			if (!IsInstanceValid(Puzzles.Value)) continue;
			if (Puzzles.Value.HasChild(pack))
			{
				Puzzles.Value.RemoveChild(pack);
				pack.QueueFree();
			}
		}
	}
	public void Fill(UI.MainMenu menu, IEnumerable<PuzzleData.Pack> packs)
	{
		foreach (PuzzleData.Pack pack in packs)
		{
			PackDisplay display = PackDisplay.Create(pack.Name, this, menu, pack.Puzzles);
			_packDisplays.Add(display);
		}
	}
	public void Fill(UI.MainMenu menu, IEnumerable<SaveData> saves)
	{
		PackDisplay display = PackDisplay.Create("Saved Puzzles", this, menu, saves);
		_packDisplays.Add(display);
	}

	sealed partial class PackDisplay : PanelContainer
	{
		public static PackDisplay Create(string name, PuzzleSelector parent, Control menu, IEnumerable<Display.Data> data)
		{
			PackDisplay display = new PackDisplay { Name = name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			display.Puzzles.Label.Text = name;
			foreach (Display.Data puzzle in data)
			{
				Color statusColor = puzzle switch
				{
					SaveData { IsComplete: true } save => Colors.Green,
					_ => Colors.Black
				};
				PuzzleDisplay puzzleDisplay = new PuzzleDisplay
				{
					Name = puzzle.Name + " Display",
					Button = new() { Name = puzzle.Name + " Button", Text = puzzle.Name },
					Background = new ColorRect { Name = "Background", Color = statusColor }
						.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
				}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
				puzzleDisplay.Button.Pressed += pressed;
				display.Puzzles.Value.Add(puzzleDisplay);

				void pressed()
				{
					if (!IsInstanceValid(parent)) return;
					PuzzleManager.Current.Puzzle = puzzle;
					parent.Hide();
					if (!IsInstanceValid(menu)) return;
					menu.Hide();
				}
			}
			parent.Puzzles.Value.AddChild(display);
			return display;
		}

		public Labelled<VBoxContainer> Puzzles { get; } = new Labelled<VBoxContainer>()
		{
			Name = "Puzzles Display",
			Label = new RichTextLabel { Name = "Label", FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = new VBoxContainer { Name = "Puzzles Container" }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill),
			Vertical = true
		}
			.Preset(LayoutPreset.FullRect);
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

