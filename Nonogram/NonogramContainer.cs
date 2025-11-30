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

		Display.Data startPuzzle = Current.Puzzle = PuzzleData.Pack.Procedural().Puzzles.First();
		Load(startPuzzle).Switch(
			Displays.CurrentTabDisplay.Load,
			error => GD.Print(error.Message),
			notFound => GD.Print("Current puzzle not found")
		);
		ToolsBar.AddPuzzles(PuzzleData.Pack.Procedural());
		ToolsBar.PuzzleLoader.Size = ToolsBar.CodeLoader.Size = GetTree().Root.Size / 2;
		ToolsBar.CodeLoader.Control.Input.TextChanged += text => PuzzleData.Code.Encode(text).Switch(
			error => ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			code => ToolsBar.CodeLoader.Control.Validation.Text = $"valid code of size: {code.Size}"
		);
		ToolsBar.CodeLoader.Control.Input.TextSubmitted += value => Load(value).Switch(
			Displays.CurrentTabDisplay.Load,
			error => ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			notFound => GD.Print("Not Found")
		);
		ToolsBar.PuzzleLoader.AboutToPopup += ToolsBar.LoadSavedPuzzles;
		ToolsBar.Saver.SetItems(
			clear: true,
			("Save Puzzle", Key.None, SavePuzzlePressed)
		);
		ToolsBar.Loader.SetItems(
			false,
			("Load Puzzle", Key.None, () => ToolsBar.PuzzleLoader.PopupCentered()),
			("Load From Code", Key.None, () => ToolsBar.CodeLoader.PopupCentered())
		//("Load Current", Key.None, () => LoadCurrent(Displays.CurrentTabDisplay))
		);
		ChildEnteredTree += node =>
		{
			switch (node)
			{
				case GameDisplay display:
					Status.CompletionLabel.Visible = true;
					break;
			}
		};
		ChildExitingTree += node =>
		{
			switch (node)
			{
				case GameDisplay display:
					Status.CompletionLabel.Visible = false;
					break;
			}
		};
		void SavePuzzlePressed()
		{
			SaveData.Create(Displays).Switch(
				save => Save(save),
				notFound => GD.Print("No current puzzle found")
			);
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
			ChildEnteredTree += node =>
			{
				if (node is not Display)
				{
					GD.PushWarning($"Child Added is not of type {typeof(Display)}, removing child {nameof(node)}");
					RemoveChild(node);
				}
			};
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
			Tiles.SetText(data.StateAsText);
			Hints.SetText(CalculateHintAt, data.HintPositions);
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
			Tiles.SetText(
				getText: data is SaveData save ? save.StateAsText : data.StateAsText
			);
			Hints.SetText(CalculateHintAt, data.HintPositions);
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
			foreach (Button button in Tiles.Values) ResetTile(button);
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
			Tiles.SetText(data.StateAsText);
			Hints.SetText(CalculateHintAt, data.HintPositions);
		}

		public override void OnTilePressed(Vector2I position)
		{
			base.OnTilePressed(position);
			Hints.SetText(asText: CalculateHintAt, position);
		}
		public override void Reset()
		{
			foreach (Button button in Tiles.Values) ResetTile(button);
			foreach (RichTextLabel label in Hints.Values) ResetHint(label);
		}
	}

}
public interface IColours { Color NonogramBackground { get; } }