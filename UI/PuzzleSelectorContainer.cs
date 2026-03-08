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
		public static PackDisplay CreateForGame((string name, IEnumerable<SaveData> data) config)
		{
			GridContainer grid = new GridContainer { Name = "Puzzles Container", Columns = 5 }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
			Labelled<Container> puzzles = CreatePuzzles(config.name, container: grid);
			PackDisplay packDisplay = new PackDisplay { Name = config.name, Puzzles = puzzles }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			return packDisplay;
		}
		public static PackDisplay CreateForStudio((string name, IEnumerable<SaveData> data) config)
		{
			VBoxContainer list = new VBoxContainer { Name = "Puzzles Container" }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
			Labelled<Container> puzzles = CreatePuzzles(config.name, container: list);
			PackDisplay packDisplay = new PackDisplay { Name = config.name, Puzzles = puzzles }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			return packDisplay;
		}
		private static Labelled<Container> CreatePuzzles<T>(string name, T container) where T : Container
		{
			return new Labelled<Container>
			{
				Name = "Puzzles Display",
				Label = new RichTextLabel { Name = "Label", Text = name, FitContent = true }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
				Value = container,
				Vertical = true
			}.Preset(LayoutPreset.FullRect);
		}

		public required Labelled<Container> Puzzles { get; init; }
		private PackDisplay() { }
		public override void _Ready() => this.Add(Puzzles);
	}
	public sealed partial class PuzzleDisplay : PanelContainer
	{
		public static PuzzleDisplay Create(SaveData puzzle, Action pressed)
		{
			Color statusColor = puzzle.CompletionColour;
			ImageTexture icon = puzzle.AsIcon(Core.Colours, 16);
			string name = puzzle.Name;

			PuzzleDisplay display = new PuzzleDisplay { Name = name + " Display" }
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);

			var background = display.Background;
			background.OverrideStyle((StyleBoxFlat style) =>
			{
				style.SetCornerRadiusAll(0);
				return style;
			});
			background.Color = statusColor;

			Button button = display.Button;
			button.OverrideStyle<StyleBoxFlat, Button>(modify);
			button.OverrideStyle<StyleBoxFlat, Button>(modify, "hover");
			button.Name = name + " Button";
			button.Text = name;
			button.Icon = icon;
			button.Pressed += pressed;

			return display;

			static StyleBoxFlat modify(StyleBoxFlat style)
			{
				style.SetCornerRadiusAll(0);
				style.SetContentMarginAll(40);
				return style;
			}
		}
		public ColorRect Background { get; } = new ColorRect { Name = "Background", }
			.Preset(LayoutPreset.LeftWide)
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		public Button Button { get; } = new()
		{
			VerticalIconAlignment = VerticalAlignment.Top,
			IconAlignment = HorizontalAlignment.Center,
		};
		public override void _Ready() => this.Add(Background, Button);
	}
}

