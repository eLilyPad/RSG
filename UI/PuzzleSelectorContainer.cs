using Godot;

namespace RSG.Nonogram;

public sealed partial class PuzzleSelector : PanelContainer
{
	private static Labelled<Container> CreatePuzzles<T>(string name, T container)
	where T : Container
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
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkCyan }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer { Name = "Scroll" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Labelled<Container> Puzzles { get; } = new Labelled<Container>
	{
		Name = "Puzzles",
		Vertical = true,
		Label = new RichTextLabel { Name = "PuzzlesTitle", FitContent = true, Text = "Puzzles" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer { Name = "Container" }
			.SizeFlags(both: SizeFlags.ExpandFill)
	}.SizeFlags(both: SizeFlags.ExpandFill);

	public override void _Ready() => this.Add(Background, Scroll.Add(Puzzles));

	public sealed partial class Studio : PanelContainer
	{
		public ScrollContainer Scroll { get; } = new ScrollContainer()
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public VBoxContainer Puzzles { get; } = new VBoxContainer()
			.SizeFlags(both: SizeFlags.ExpandFill);
		public override void _Ready() => this.Add(Scroll.Add(Puzzles));
	}

	public abstract partial class PackDisplay : PanelContainer
	{
		public static PackDisplay CreateForGame((string Name, IEnumerable<SaveData> Data) config)
		{
			return new GamePacks { Name = config.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		}
		public static PackDisplay CreateForStudio((string Name, IEnumerable<SaveData> Data) config)
		{
			return new StudioPacks { Name = config.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		}
		public new string Name
		{
			get => base.Name;
			set => base.Name = (Puzzles.Label.Text = value) + " Pack";
		}
		public virtual Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
			"Puzzles",
			container: new VBoxContainer { Name = "List" }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
		);
		public override void _Ready() => this.Add(Puzzles);

	}
	private sealed partial class GamePacks : PackDisplay
	{
		public override Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
			"Puzzles",
			container: new GridContainer { Name = "Grid", Columns = 5 }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
		);
	}
	private sealed partial class StudioPacks : PackDisplay;
	public partial class PuzzleDisplay : PanelContainer
	{
		public static PuzzleDisplay CreateGameDisplay(SaveData puzzle, Action pressed)
		{
			return new GamePuzzleDisplay(puzzle, pressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		}
		public static PuzzleDisplay CreateStudioDisplay(SaveData puzzle, Action pressed)
		{
			return new StudioPuzzleDisplay(puzzle, pressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		}
		public ColorRect Background { get; } = new ColorRect { Name = "Background", }
		.Preset(LayoutPreset.LeftWide)
		.SizeFlags(both: SizeFlags.ExpandFill)
		.OverrideStyle((StyleBoxFlat style) =>
		{
			style.SetCornerRadiusAll(0);
			return style;
		});
		public Button Button { get; } = new Button
		{
			VerticalIconAlignment = VerticalAlignment.Top,
			IconAlignment = HorizontalAlignment.Center,
		}
		.OverrideStyle<StyleBoxFlat, Button>(Modify)
		.OverrideStyle<StyleBoxFlat, Button>(Modify, "hover");
		public override void _Ready() => this.Add(Background, Button);
		static StyleBoxFlat Modify(StyleBoxFlat style)
		{
			style.SetCornerRadiusAll(0);
			style.SetContentMarginAll(40);
			return style;
		}
	}
	private sealed partial class GamePuzzleDisplay(SaveData Puzzle, Action Pressed) : PuzzleDisplay
	{
		public override void _Ready()
		{
			base._Ready();
			Name = Puzzle.Name;

			Background.Color = Puzzle.CompletionColour;

			Button.Name = Puzzle.Name + " Button";
			Button.Text = Puzzle.IsComplete ? Puzzle.Name : string.Empty;
			Button.Icon = Puzzle.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
		}
	}
	private sealed partial class StudioPuzzleDisplay(SaveData Puzzle, Action Pressed) : PuzzleDisplay
	{
		public override void _Ready()
		{
			base._Ready();
			Name = Puzzle.Name;

			Background.Color = Puzzle.CompletionColour;

			Button.Name = Name + " Button";
			Button.Text = Name;
			Button.Icon = Puzzle.Expected.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
		}
	}
}

