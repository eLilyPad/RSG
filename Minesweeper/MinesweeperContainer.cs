using Godot;

namespace RSG.Minesweeper;

public sealed partial class MinesweeperContainer : HBoxContainer
{
	public sealed partial class CompletionOptions : HBoxContainer
	{
		public Button Levels { get; } = new() { Name = "Levels", Text = "Levels" };
		public Button Dialogues { get; } = new() { Name = "Dialogue", Text = "Dialogues" };
		public Button PlayDialogue { get; } = new() { Name = "PlayDialogue", Text = "Play Dialogue" };
		public override void _Ready() => this.Add(Levels, Dialogues, PlayDialogue);
	}
	public sealed partial class CompletionReport : VBoxContainer
	{
		public RichTextLabel Title { get; } = new()
		{
			Name = "title",
			FitContent = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			BbcodeEnabled = true,
			Text = "[font_size=50]Unlocked"
		};
		public RichTextLabel Log { get; } = new()
		{
			Name = "Log",
			FitContent = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			BbcodeEnabled = true,
		};
		public override void _Ready() => this.Add(Title, Log);
	}
	public sealed partial class CompletedScreen : VBoxContainer
	{
		public CompletionOptions Options { get; } = new CompletionOptions
		{
			Name = "Options",
			SizeFlagsStretchRatio = .3f,
			Alignment = AlignmentMode.Center
		}
			.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ShrinkCenter)
			.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
		public AspectRatioContainer ReportContainer { get; } = new AspectRatioContainer { Name = "Report Container" }
			.Preset(LayoutPreset.FullRect)
			.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
		public Backgrounded<CompletionReport> Report { get; } = new Backgrounded<CompletionReport>()
		{
			Name = "Report",
			SizeFlagsStretchRatio = .3f,
			Background = new ColorRect { Color = Colors.DarkBlue with { A = .5f } }
				.Preset(LayoutPreset.FullRect),
			Value = new CompletionReport() { Name = "Value Report" }
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
		}.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
		public RichTextLabel CompletionTitle { get; } = new RichTextLabel
		{
			Name = "Completion Title",
			Text = "[color=black][font_size=80] Puzzle Complete",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			SizeFlagsStretchRatio = .2f,
			BbcodeEnabled = true,
			FitContent = true,
		}
			.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
			.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(CompletionTitle, ReportContainer.Add(Report), Options);
	}
	public sealed partial class MinesweeperBackground : Container
	{
		public ColorRect ColorBackground { get; } = new ColorRect { Color = Colors.AntiqueWhite with { A = 0.3f } }
		.Preset(LayoutPreset.FullRect);
		public TextureRect Border { get; } = new TextureRect { Name = "Border", ClipContents = true }
			.Preset(LayoutPreset.FullRect);
		public override void _Ready() => this.Add(ColorBackground, Border);
	}
	public sealed partial class MinesweeperDisplay : AspectRatioContainer
	{
		public const MouseButton CheckButton = MouseButton.Left, FlagButton = MouseButton.Right;

		public MarginContainer Margin { get; } = new MarginContainer { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public GridContainer TilesGrid { get; } = new GridContainer { Name = "Tiles", Columns = 2 }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

		public override void _Ready() => this.Add(Margin.Add(TilesGrid));
	}
	public Backgrounded<CompletedScreen> CompletionScreen { get; } = new Backgrounded<CompletedScreen>
	{
		Name = "PuzzleCompleteScreen",
		Visible = false,
		Background = new ColorRect { Name = "Background", Color = Colors.SlateGray with { A = .7f } }
			.Preset(LayoutPreset.FullRect),
		Value = new CompletedScreen { Name = "Value" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize, 250)
	}.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.Minsize);

	public MinesweeperBackground Background { get; } = new MinesweeperBackground { Name = "Background" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MinesweeperDisplay Display { get; } = new MinesweeperDisplay { }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);

	public int PuzzleSize
	{
		set
		{
			Tiles.Update(value);
			Display.TilesGrid.Columns = value;
			Display.TilesGrid.CustomMinimumSize = value * Tiles.TileSize;
		}
	}

	internal Tile.Pool Tiles { get; }

	internal MinesweeperContainer(IColours colours)
	{
		Tiles = new(parent: Display.TilesGrid, colours);
	}
	public override void _Ready() => this.Add(Background, Display, CompletionScreen);
}

