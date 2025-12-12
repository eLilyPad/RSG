using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class NonogramContainer : Container
{
	public interface IHaveTools { PopupMenu Tools { get; } }
	public interface IHaveStatus { StatusBar Status { get; } }

	public sealed partial class DisplayContainer : TabContainer
	{
		public List<Display> Tabs { internal get; init; } = [];
		public Display CurrentTabDisplay => GetCurrentTabControl() is not Display display ? Tabs.First() : display;
	}
	public sealed partial class StatusBar : HBoxContainer
	{
		public const string PuzzleComplete = "Puzzle is complete", PuzzleIncomplete = "Puzzle is incomplete";
		public RichTextLabel CompletionLabel { get; } = new RichTextLabel
		{
			Name = "Completion",
			FitContent = true,
			CustomMinimumSize = new(200, 0),
			Text = PuzzleIncomplete
		}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(CompletionLabel);
	}
	public sealed partial class PuzzleCompleteScreen : PanelContainer
	{
		public ColorRect Background { get; } = new ColorRect
		{
			Name = "Background",
			Color = Colors.DarkGray,
		}
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public RichTextLabel CompletionTitle { get; } = new RichTextLabel
		{
			Name = "Completion Title",
			BbcodeEnabled = true,
			Text = "[color=black][font_size=60] Puzzle Complete",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			FitContent = true,
		}
			.Preset(preset: LayoutPreset.Center, resizeMode: LayoutPresetMode.KeepSize);
		public VBoxContainer Container { get; } = new VBoxContainer { Name = "Completion Container" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public HBoxContainer Options { get; } = new HBoxContainer { Name = "Options Container" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public Button Levels { get; } = new() { Name = "LevelsButton", Text = "Levels" };
		public override void _Ready()
		{
			this.Add(
				Background,
				Container.Add(CompletionTitle, Options.Add(Levels))
			);
		}
	}
	public sealed partial class GameDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Game" };
		public required PuzzleCompleteScreen CompletionScreen { get; init; }
		public required StatusBar Status { get; init; }

		public override void Load(Data data)
		{
			base.Load(data);
			Reset();
			if (data is not SaveData save) return;
			WriteToTiles(save);
		}
		public override void OnTilePressed(Vector2I position)
		{
			if (!Tiles.TryGetValue(position, out Tile? button)) return;
			bool
			block = Input.IsMouseButtonPressed(BlockButton),
			fill = Input.IsMouseButtonPressed(FillButton);
			if (block)
			{
				button.Button.Text = BlockText;
				Audio.Buses.SoundEffects.Play(Audio.NonogramSounds.BlockTileClicked);
			}
			else if (fill)
			{
				button.Button.Text = FillText;
				Audio.Buses.SoundEffects.Play(Audio.NonogramSounds.FillTileClicked);
			}
			Current.SaveProgress();
		}
		public override void Reset()
		{
			foreach (Tile button in Tiles.Values) ResetTile(button);
		}
	}
	public sealed partial class PaintDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Paint" };

		public override void OnTilePressed(Vector2I position)
		{
			base.OnTilePressed(position);
			WriteToHints(positions: HintPosition.Convert(position));
		}
		public override void Reset()
		{
			foreach (Tile button in Tiles.Values) ResetTile(button);
			foreach (Hint label in Hints.Values) ResetHint(label);
		}
	}

	public Menu ToolsBar { get; } = new() { Name = "Toolbar", SizeFlagsStretchRatio = 0.05f };
	public StatusBar Status { get; } = new StatusBar { Name = "Status Bar", SizeFlagsStretchRatio = 0.05f }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(LayoutPreset.BottomWide, LayoutPresetMode.KeepWidth);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(.2f, .3f, 0) }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public DisplayContainer Displays => field ??= new DisplayContainer { Name = $"{typeof(Display)} Tabs", TabsVisible = true }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready() => this.Add(Background, Container.Add(ToolsBar, Displays, Status));
}
public interface IColours { Color NonogramBackground { get; } }