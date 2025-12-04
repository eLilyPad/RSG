using Godot;

namespace RSG.Nonogram;

using static PuzzleManager;

public sealed partial class NonogramContainer : Container
{
	private static Menu InitMenu(Menu menu, DisplayContainer displays)
	{
		menu.PuzzleLoader.Size = menu.CodeLoader.Size = menu.GetTree().Root.Size / 2;
		menu.Saver.SetItems(
			clear: true,
			("Save Puzzle", Key.None, SavePuzzlePressed)
		);
		menu.Loader.SetItems(
			false,
			("Load Puzzle", Key.None, () => menu.PuzzleLoader.PopupCentered()),
			("Load From Code", Key.None, () => menu.CodeLoader.PopupCentered())
		//("Load Current", Key.None, () => LoadCurrent(Displays.CurrentTabDisplay))
		);

		menu.CodeLoader.Control.Input.TextChanged += OnCodeChanged;
		menu.CodeLoader.Control.Input.TextSubmitted += OnCodeSubmitted;
		menu.PuzzleLoader.AboutToPopup += LoadSavedPuzzles;

		return menu;

		void OnCodeSubmitted(string value) => Load(value).Switch(
			displays.CurrentTabDisplay.Load,
			error => menu.CodeLoader.Control.Validation.Text = error.Message,
			notFound => GD.Print("Not Found")
		);
		void OnCodeChanged(string value) => PuzzleData.Code.Encode(value).Switch(
			error => menu.CodeLoader.Control.Validation.Text = error.Message,
			code => menu.CodeLoader.Control.Validation.Text = $"valid code of size: {code.Size}"
		);
		void SavePuzzlePressed()
		{
			SaveData.Create(displays).Switch(
				save => Save(save),
				notFound => GD.Print("No current puzzle found")
			);
		}
		void LoadSavedPuzzles()
		{
			menu.PuzzleLoader.Control.Saved.RemoveChildren(free: true);
			foreach (SaveData puzzle in GetSavedPuzzles())
			{
				Button button = new() { Text = puzzle.Name };
				button.Pressed += () => Current.Puzzle = puzzle;
				menu.PuzzleLoader.Control.Saved.Add(button);
			}
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
	public DisplayContainer Displays
	{
		get => field ??= new DisplayContainer(
			menu: ToolsBar,
			new GameDisplay { Status = Status },
			new PaintDisplay { },
			new ExpectedDisplay { }
		);
	}

	public override void _Ready()
	{
		this.Add(Background, Container.Add(ToolsBar, Displays, Status))
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		PuzzleData.Pack proceduralPuzzles = PuzzleData.Pack.Procedural();
		Display.Data startPuzzle = Current.Puzzle = proceduralPuzzles.Puzzles.First();
		Load(startPuzzle).Switch(
			Displays.CurrentTabDisplay.Load,
			error => GD.Print(error.Message),
			notFound => GD.Print("Current puzzle not found")
		);
		ToolsBar.AddPuzzles(proceduralPuzzles);

		InitMenu(ToolsBar, Displays);

		ChildEnteredTree += OnChildEnteringTree;
		ChildExitingTree += OnChildExitingTree;

		void OnChildEnteringTree(Node node)
		{
			if (Displays.HasChild(node) && node is not Display)
			{
				GD.PushWarning($"Child Added is not of type {typeof(Display)}, removing child {nameof(node)}");
				Displays.RemoveChild(node);
			}
			switch (node)
			{
				case GameDisplay display:
					Status.CompletionLabel.Visible = true;
					break;
			}
		}
		void OnChildExitingTree(Node node)
		{
			switch (node)
			{
				case GameDisplay display:
					Status.CompletionLabel.Visible = false;
					break;
			}
		}
	}

	public interface IHaveTools { PopupMenu Tools { get; } }

	public sealed partial class DisplayContainer : TabContainer
	{
		private static class Errors
		{
			public static class Construction
			{
				public const string NoDisplayGiven = "No Displays given, provide at least one display when creating the Container";
			}
		}
		public List<Display> Tabs { private get; init; } = [];
		public Display CurrentTabDisplay => GetCurrentTabControl() is not Display display ? Tabs.First() : display;
		public DisplayContainer(Menu menu, params List<Display> displays)
		{
			Assert(condition: displays.Count != 0, message: Errors.Construction.NoDisplayGiven);

			Name = $"{typeof(Display)} Tabs";
			TabsVisible = true;

			foreach (Display display in displays)
			{
				Tabs.Add(display);
				AddChild(display);
				if (Current.Puzzle is null) { continue; }
				display.Load(Current.Puzzle);
			}

			TabChanged += OnTabChanged;
			OnTabChanged(CurrentTab);

			void OnTabChanged(long index)
			{
				Display current = Current.Display = CurrentTabDisplay;
				current.Load(Current.Puzzle);
				foreach (Display other in displays.Except([current]))
				{
					if (other == current
						|| other is not IHaveTools { Tools: PopupMenu otherTools }
						|| !menu.HasChild(otherTools)
					) continue;
					menu.RemoveChild(otherTools);
				}
				if (current is not IHaveTools { Tools: PopupMenu currentTools } || menu.HasChild(currentTools)) { return; }
				menu.AddChild(currentTools);
			}
		}
		public override void _Ready()
		{
			this.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}
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
		public ExpectedDisplay()
		{
			Name = "Expected";
		}
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
			Name = "Game";
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
			Name = "Painter";
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