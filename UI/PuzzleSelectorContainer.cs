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

	public override void _Ready() => this.Add(Background, Scroll.Add(Puzzles));

	public sealed partial class PackDisplay : PanelContainer
	{
		public readonly record struct Config(string Name, IEnumerable<SaveData> Data);
		public static Func<Config, CanvasItem, PackDisplay> Create(CurrentPuzzle current)
		{
			return (config, root) => Create(config, root, current);
		}
		public static PackDisplay Create(Config config, CanvasItem root, CurrentPuzzle current)
		{
			PackDisplay display = new() { Name = config.Name };
			foreach (SaveData save in config.Data)
			{
				PuzzleDisplay puzzle = PuzzleDisplay.Create(save, root, current);
				display.Puzzles.Value.AddChild(puzzle);
			}

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
		}.Preset(LayoutPreset.FullRect);

		public new StringName Name
		{
			get => base.Name;
			set => Puzzles.Label.Text = base.Name = value;
		}
		internal PackDisplay() { }
		public override void _Ready() => this
			.Add(Puzzles)
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
	}
	public sealed partial class PuzzleDisplay : PanelContainer
	{
		public static PuzzleDisplay Create(Display.Data puzzle, CanvasItem root, CurrentPuzzle current)
		{
			Color statusColor = puzzle switch
			{
				SaveData { IsComplete: true } => Colors.Green,
				_ => Colors.Black
			};
			PuzzleDisplay display = new PuzzleDisplay
			{
				Name = puzzle.Name + " Display",
				Button = new() { Name = puzzle.Name + " Button", Text = puzzle.Name },
				Background = new ColorRect { Name = "Background", Color = statusColor }
					.Preset(LayoutPreset.LeftWide)
					.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

			if (puzzle is SaveData save)
			{
				display.Button.Pressed += pressed;

				void pressed()
				{
					if (!IsInstanceValid(root)) return;
					current.Puzzle = save;
					current.UI.Show();
					root.Hide();
				}
			}

			display.Button.OverrideStyle((StyleBoxFlat style) =>
			{
				style.SetCornerRadiusAll(0);
				return style;
			});
			display.Background.OverrideStyle((StyleBoxFlat style) =>
			{
				style.ContentMarginBottom = 50;
				style.SetCornerRadiusAll(0);
				return style;
			});

			return display;
		}
		public required ColorRect Background { get; init; }
		public required Button Button { get; init; }
		public override void _Ready() => this.Add(Background, Button);
	}
}

