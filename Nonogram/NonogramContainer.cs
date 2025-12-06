using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class NonogramContainer : Container
{
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

	public override void _Ready()
	{
		this.Add(Background, Container.Add(ToolsBar, Displays, Status));
	}

	public interface IHaveTools { PopupMenu Tools { get; } }

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
	public sealed partial class ExpectedDisplay : Display
	{
		public override void OnTilePressed(Vector2I position) { }
		public override void Reset() { }
		public override void Load(Data data)
		{
			ChangePuzzleSize(data.Size);
			WriteToTiles(data);
			foreach (HintPosition position in data.HintPositions)
			{
				if (!Hints.TryGetValue(position, out Hint? hint)) { continue; }
				hint.Text = CalculateHintAt(position);
			}
		}
	}
	public sealed partial class GameDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Game" };
		public required StatusBar Status { get; init; }
		public GameDisplay()
		{
			Tools.SetItems(
				clear: false,
				("Reset", Key.None, Reset)
			);
		}
		public override void Load(Data data)
		{
			ChangePuzzleSize(data.Size);
			WriteToTiles(data);
			WriteToHints(data.HintPositions);
			Reset();
		}
		public override void OnTilePressed(Vector2I position)
		{
			base.OnTilePressed(position);
			Audio.Buses.SoundEffects.Play(Audio.NonogramSounds.TileClicked);
			if (Current.Puzzle is null)
			{
				GD.PushWarning("No Puzzle Selected unable to check if completed");
				return;
			}
			if (!Current.Puzzle.Matches(this))
			{
				Status.CompletionLabel.Text = StatusBar.PuzzleIncomplete;
				return;
			}
			Status.CompletionLabel.Text = StatusBar.PuzzleComplete;

		}
		public override void Reset()
		{
			foreach (Tile button in Tiles.Values) ResetTile(button);
		}
	}
	public sealed partial class PaintDisplay : Display, IHaveTools
	{
		public PopupMenu Tools { get; } = new() { Name = "Paint" };

		public PaintDisplay()
		{
			Tools.SetItems(
				clear: false,
				("Reset", Key.None, Reset)
			);
		}
		public override void Load(Data data)
		{
			ChangePuzzleSize(data.Size);
			WriteToTiles(data);
			WriteToHints(positions: data.HintPositions);
		}
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
}
public interface IColours { Color NonogramBackground { get; } }