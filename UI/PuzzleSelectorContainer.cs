using Godot;

namespace RSG.Nonogram;

public sealed partial class PuzzleSelector : PanelContainer
{
	public sealed partial class Studio : PanelContainer
	{
		public ScrollContainer Scroll { get; } = new ScrollContainer()
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public VBoxContainer Puzzles { get; } = new VBoxContainer()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public override void _Ready() => this.Add(Scroll.Add(Puzzles));
	}
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
		public static PackDisplay Create((string name, IEnumerable<SaveData> data) config, CanvasItem root)
		{
			return new PackDisplay { Name = config.name, Puzzles = CreateSelectorPuzzles(config.name) }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
				.AddPuzzles(config.data, root);
		}
		public static PackDisplay CreateForStudio((string name, IEnumerable<SaveData> data) config, CanvasItem root)
		{
			return new PackDisplay { Name = config.name, Puzzles = CreateStudioPuzzles(config.name) }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
				.AddPuzzles(config.data, root);
		}
		public static PackDisplay Create(string name, CanvasItem root, IEnumerable<SaveData> data)
		{
			return new PackDisplay { Name = name, Puzzles = CreateSelectorPuzzles(name) }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
				.AddPuzzles(data, root);
		}

		private static Labelled<Container> CreateStudioPuzzles(string name) => new Labelled<Container>()
		{
			Name = "Puzzles Display",
			Label = new RichTextLabel { Name = "Label", Text = name, FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = new VBoxContainer { Name = "Puzzles Container" }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill),
			Vertical = true
		}.Preset(LayoutPreset.FullRect);
		private static Labelled<Container> CreateSelectorPuzzles(string name) => new Labelled<Container>()
		{
			Name = "Puzzles Display",
			Label = new RichTextLabel { Name = "Label", Text = name, FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = new GridContainer { Name = "Puzzles Container", Columns = 5 }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill),
			Vertical = true
		}.Preset(LayoutPreset.FullRect);


		public required Labelled<Container> Puzzles { get; init; }
		private PackDisplay() { }
		public override void _Ready() => this.Add(Puzzles);
		private PackDisplay AddPuzzles(IEnumerable<SaveData> saves, CanvasItem root)
		{
			foreach (SaveData puzzle in saves)
			{
				Puzzles.Value.Add(PuzzleDisplay.Create(puzzle, root));
			}
			return this;
		}
	}
	public sealed partial class PuzzleDisplay : PanelContainer
	{
		public static PuzzleDisplay Create(SaveData puzzle, CanvasItem root)
		{
			PuzzleDisplay display = Create(puzzle);
			display.Button.Pressed += pressed;
			return display;
			void pressed()
			{
				if (!IsInstanceValid(root)) return;
				PuzzleManager.Current.Puzzle = puzzle;
				PuzzleManager.Current.UI.Show();
				root.Hide();
			}
		}
		public static PuzzleDisplay Create(SaveData puzzle)
		{
			Color statusColor = puzzle switch
			{
				{ IsComplete: true } => Colors.Green,
				_ => Colors.Black
			};
			PuzzleDisplay display = new PuzzleDisplay
			{
				Name = puzzle.Name + " Display",
				Button = new()
				{
					Name = puzzle.Name + " Button",
					Text = puzzle.Name,
					VerticalIconAlignment = VerticalAlignment.Top,
					IconAlignment = HorizontalAlignment.Center,
					Icon = puzzle.AsIcon(colours: Core.Colours, pixelSize: 16),
				},
				Background = new ColorRect { Name = "Background", Color = statusColor }
					.Preset(LayoutPreset.LeftWide)
					.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
			}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

			display.Button.OverrideStyle<StyleBoxFlat, Button>(modify);
			display.Button.OverrideStyle<StyleBoxFlat, Button>(modify, "hover");
			display.Background.OverrideStyle((StyleBoxFlat style) =>
			{
				style.SetCornerRadiusAll(0);
				return style;
			});

			return display;

			static StyleBoxFlat modify(StyleBoxFlat style)
			{
				style.SetCornerRadiusAll(0);
				style.SetContentMarginAll(40);
				return style;
			}
		}
		public required ColorRect Background { get; init; }
		public required Button Button { get; init; }
		public override void _Ready() => this.Add(Background, Button);
	}
}

