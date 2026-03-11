using Godot;

namespace RSG.Nonogram;

public sealed partial class PuzzleSelector : PanelContainer
{
	public static PackDisplay CreateGamePack(
		string name,
		Control parent,
		List<PackDisplay> packs
	)
	{
		GamePacks pack = new GamePacks { Name = name }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		parent.Add(pack);
		packs.Add(pack);

		return pack;
	}
	public static PackDisplay CreateStudioPack(
		string name,
		List<PackDisplay> packs,
		Control parent
	)
	{
		StudioPacks pack = new StudioPacks { Name = name }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		packs.Add(pack);
		parent.Add(pack);
		return pack;
	}
	public static PuzzleDisplay CreateGameDisplay(
		SaveData puzzle,
		Control parent,
		UI.MainMenu menu,
		IHandleDisplaysPressed handler
	)
	{
		GamePuzzleDisplay display = new GamePuzzleDisplay(puzzle, pressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		parent.Add(display);
		return display;
		void pressed() => handler.GamePuzzleDisplayPressed(menu, puzzle);
	}
	public static PuzzleDisplay CreateStudioDisplay(
		SaveData puzzle,
		List<PuzzleDisplay> values,
		Control parent,
		UI.MainMenu menu,
		IHandleDisplaysPressed handler
	)
	{
		StudioPuzzleDisplay display = new StudioPuzzleDisplay(puzzle, pressed, altPressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		values.Add(display);
		parent.Add(display);
		return display;

		void pressed() => handler.StudioPuzzleDisplayPressed(puzzle);
		void altPressed()
		{
			bool rightClicked = Input.IsMouseButtonPressed(MouseButton.Right);
			if (!rightClicked) return;
			handler.GamePuzzleDisplayPressed(menu, puzzle);
		}
	}

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
			Button.Text = Puzzle.Name;
			Button.Icon = Puzzle.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
		}
	}
	private sealed partial class StudioPuzzleDisplay(SaveData Puzzle, Action Pressed, Action AltPressed)
		: PuzzleDisplay
	{
		public override void _Ready()
		{
			base._Ready();
			Background.Color = Puzzle.CompletionColour;
			Button.Name = (Button.Text = Name = Puzzle.Name) + " Button";
			Button.Icon = Puzzle.Expected.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
			Button.GuiInput += _ => AltPressed();
		}
	}
}

