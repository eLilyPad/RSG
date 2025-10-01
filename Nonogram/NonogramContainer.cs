using Godot;
using RSG.Nonogram;

namespace RSG.UI.Nonogram;

using static PuzzleManager;

public sealed partial class NonogramContainer : Container
{
	public Menu ToolsBar { get; } = new();
	public StatusBar Status { get; } = new StatusBar()
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = new(.2f, .3f, 0) }
	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public DisplayContainer Displays
	{
		get
		{
			GameDisplay game = new() { Status = Status };
			PaintDisplay paint = new();
			return field ??= new DisplayContainer(menu: ToolsBar, game, paint);
		}
	}

	public override void _Ready()
	{
		this.Add(Background, Container.Add(ToolsBar, Displays, Status))
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

		//LoadCurrent(Displays.CurrentTabDisplay);
		Load().Switch(
			error => GD.Print(error.Message),
			Displays.CurrentTabDisplay.Load,
			save => Displays.CurrentTabDisplay.Load(save.Expected, save.Current),
			exception => GD.Print(exception),
			notFound => GD.Print("Current puzzle not found")
		);
		ToolsBar.CodeLoader.Input.TextSubmitted += CodeSubmitted;
		ToolsBar.PuzzleLoaderPopup.AboutToPopup += () => ToolsBar.PuzzleLoader.AddPuzzles(
			data: PuzzleData.Pack.Procedural().Puzzles,
			display: Displays.CurrentTabDisplay
		);
		ToolsBar.Saver.SetItems(
			clear: true,
			("Save Puzzle", Key.None, SavePuzzlePressed)
		);
		ToolsBar.Loader.SetItems(
			false,
			("Load Puzzle", Key.None, () => ToolsBar.PuzzleLoaderPopup.PopupCentered()),
			("Load From Code", Key.None, () => ToolsBar.CodeLoaderPopup.PopupCentered())
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
			PuzzleData.Savable.Create(Displays).Switch(
				save => Save(save),
				notFound => GD.Print("No current puzzle found")
			);
		}
		void CodeSubmitted(string value)
		{
			Load(value).Switch(
				error => ToolsBar.CodeLoader.Validation.Text = error.Message,
				Displays.CurrentTabDisplay.Load,
				save => Displays.CurrentTabDisplay.Load(save.Expected, save.Current),
				exception => GD.Print(exception),
				notFound => GD.Print("Not Found")
			);
		}
	}

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
			TabsVisible = false;

			foreach (Display display in displays)
			{
				Tabs.Add(display);
				AddChild(display);
				if (CurrentPuzzle is null) { continue; }
				display.Load(CurrentPuzzle);
			}

			TabChanged += OnTabChanged;
			OnTabChanged(CurrentTab);

			void OnTabChanged(long index)
			{
				Display current = CurrentTabDisplay;
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
	public sealed partial class Menu : MenuBar
	{
		public partial class PuzzleLoaderContainer : VBoxContainer
		{
			public List<Button> Puzzles { private get; init; } = [];

			public void AddPuzzles(IEnumerable<PuzzleData> data, Display display)
			{
				Clear();
				foreach (PuzzleData puzzle in data)
				{
					Button button = new() { Text = puzzle.Name };
					button.Pressed += LoadPuzzle;

					void LoadPuzzle()
					{
						CurrentPuzzle = puzzle;
						display.Load(puzzle);
					}
					Puzzles.Add(button);
					this.Add(button);
				}
			}
			public void Clear()
			{
				foreach (Button puzzle in Puzzles)
				{
					this.Remove(free: true, puzzle);
				}
			}
		}
		public partial class CodeLoaderContainer : VBoxContainer
		{
			public LineEdit Input { get; } = new LineEdit
			{
				Name = "CodeInput",
				PlaceholderText = "Enter Nonogram Code Here",
				Text = ""
			}.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize);
			public RichTextLabel Validation { get; } = new RichTextLabel
			{
				Name = "Validation",
				FitContent = true,
				Text = "Empty"
			}.Preset(LayoutPreset.BottomLeft, LayoutPresetMode.KeepSize);
			public override void _Ready()
			{
				Name = "Container";
				this.Add(Input, Validation)
					.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
				Input.TextChanged += text => PuzzleData.Code.Encode(text).Switch(
					error => Validation.Text = error.Message,
					code => Validation.Text = $"valid code of size: {code.Size}"
				);
			}
		}

		public PopupMenu Saver => field ??= new PopupMenu { Name = "Save" };
		public PopupMenu Loader => field ??= new PopupMenu { Name = "Load" }
		.Add(PuzzleLoaderPopup.Add(PuzzleLoader), CodeLoaderPopup.Add(CodeLoader));
		public CodeLoaderContainer CodeLoader { get; } = new();
		public PuzzleLoaderContainer PuzzleLoader { get; } = new();
		public Popup CodeLoaderPopup { get; } = new Popup { Name = "Code Loader" };
		public Popup PuzzleLoaderPopup { get; } = new Popup { Name = "Puzzle Loader" };

		public override void _Ready()
		{
			Name = "Toolbar";
			SizeFlagsStretchRatio = 0.05f;
			PuzzleLoaderPopup.Size = CodeLoaderPopup.Size = GetTree().Root.Size / 2;
			this.Add(Loader, Saver)
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
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
		public override void _Ready()
		{
			this.Add(CompletionLabel)
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
				.Preset(LayoutPreset.BottomWide, LayoutPresetMode.KeepWidth);
			SizeFlagsStretchRatio = 0.05f;
		}
	}
	public interface IHaveTools { PopupMenu Tools { get; } }
	//public interface IHaveStatus
	//{
	//	public sealed partial class Container : HBoxContainer { }
	//	public sealed partial class Display : RichTextLabel { }

	//	Container Bar
	//}
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
		public override void OnTilePressed(Vector2I position)
		{
			base.OnTilePressed(position);
			if (CurrentPuzzle is null)
			{
				GD.PushWarning("No Puzzle Selected unable to check if completed");
				return;
			}
			if (!CurrentPuzzle.Matches(this))
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
		public override void Load(Data data)
		{
			base.Load(data);
			Reset();
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
		public override void OnTilePressed(Vector2I position)
		{
			base.OnTilePressed(position);
			WriteToHint(position);
		}
		public override void Reset()
		{
			foreach (Button button in Tiles.Values) ResetTile(button);
			foreach (RichTextLabel label in Hints.Values) ResetHint(label);
		}
	}

}
public interface IColours { Color NonogramBackground { get; } }