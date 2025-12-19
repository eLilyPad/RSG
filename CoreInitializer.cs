using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using static Nonogram.PuzzleManager;
using static Nonogram.NonogramContainer;

public static class CoreInitializer
{
	private static class Errors
	{
		public const string NoDisplayGiven = "No Displays given, provide at least one display when creating the Container";
		public const string OutsideOfTree = "Outside of Scene Tree unable to initialize";
	}

	public static MainMenu Init(this MainMenu menu, ColourPack colours)
	{
		Assert(condition: menu.IsInsideTree(), $"{nameof(MainMenu)}- {Errors.OutsideOfTree}");

		menu.Background.Color = colours.NonogramBackground;

		menu.Buttons.Play.Pressed += PlayPressed;
		menu.Buttons.Levels.Pressed += LevelPressed;
		menu.Buttons.Dialogues.Pressed += DialoguePressed;
		menu.Buttons.Settings.Pressed += SettingsPressed;
		menu.Buttons.Quit.Pressed += QuitPressed;

		menu.Settings.VisibilityChanged += () => menu.Buttons.Visible = !menu.Settings.Visible;
		menu.Levels.VisibilityChanged += FillPuzzleSelector;
		menu.Dialogues.VisibilityChanged += FillDialogueSelector;

		return menu;

		void PlayPressed() => menu.Hide();
		void LevelPressed() => menu.Levels.Show();
		void DialoguePressed() => menu.Dialogues.Show();
		void SettingsPressed() => menu.Settings.Show();
		void QuitPressed() => menu.GetTree().Quit();

		void FillPuzzleSelector()
		{
			if (!menu.Levels.Visible) return;
			menu.Levels.ClearPacks();
			menu.Levels.Fill(menu, saves: GetSavedPuzzles());
			menu.Levels.Fill(menu, packs: GetPuzzlePacks());
		}
		void FillDialogueSelector()
		{
			if (!menu.Dialogues.Visible) return;
			menu.Dialogues.Clear();
			menu.Dialogues.Fill(Dialogues.AvailableDialogues);
		}
	}
	public static NonogramContainer Init(this NonogramContainer container, MainMenu menu)
	{
		Assert(condition: container.IsInsideTree(), $"{nameof(NonogramContainer)}- {Errors.OutsideOfTree}");

		Display.Data startPuzzle = Current.Puzzle;
		GameDisplay game = new()
		{
			Name = "Game",
			Status = container.Status,
			CompletionScreen = new PuzzleCompleteScreen { Name = "PuzzleCompleteScreen", Visible = false }
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
		};
		PaintDisplay paint = new() { Name = "Paint", };

		container.AddChild(game.CompletionScreen);
		container.ChildEnteredTree += OnChildEnteringTree;
		container.ChildExitingTree += OnChildExitingTree;
		game.CompletionScreen.Levels.Pressed += OnLevelsPressed;

		container.Displays.Init(menu: container.ToolsBar, game, paint, Display.Default);
		container.ToolsBar.Init(container.Displays);

		container.Displays.CurrentTabDisplay.Load(Current.Puzzle);

		Vector2I guideSize = (Vector2I)game.TilesGrid.Size / 2;
		guideSize = guideSize with { Y = guideSize.X };
		game.Guides.CreateLines(size: guideSize);
		paint.Guides.CreateLines(size: guideSize);
		Display.Default.Guides.CreateLines(size: guideSize);

		return container;

		void OnLevelsPressed()
		{
			menu.Show();
			menu.Levels.Show();
			game.CompletionScreen.Hide();
		}
		void OnChildEnteringTree(Node node)
		{
			switch (node)
			{
				case GameDisplay display:
					container.Status.CompletionLabel.Visible = true;
					break;
				case Display: break;
				case Node when container.Displays.HasChild(node):
					GD.PushWarning($"Child Added is not of type {typeof(Display)}, removing child {nameof(node)}");
					container.Displays.RemoveChild(node);
					break;
			}
		}
		void OnChildExitingTree(Node node)
		{
			switch (node)
			{
				case GameDisplay display:
					container.Status.CompletionLabel.Visible = false;
					break;
			}
		}
	}

	private static Menu Init(this Menu menu, DisplayContainer displays)
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
		menu.PuzzleLoader.AboutToPopup += LoadPuzzles;

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
		void LoadPuzzles()
		{
			menu.PuzzleLoader.Control.RemoveChildren(free: true);
			menu.PuzzleLoader.Control.Saved.RemoveChildren(free: true);
			foreach (SaveData puzzle in GetSavedPuzzles())
			{
				string status = puzzle.IsComplete ? " - complete" : "";
				Button button = new() { Text = $"{puzzle.Name}{status}" };
				button.Pressed += () => Current.Puzzle = puzzle;
				menu.PuzzleLoader.Control.Saved.Add(button);
			}
			foreach (PuzzleData.Pack pack in GetPuzzlePacks())
			{
				Menu.PuzzleContainer container = new Menu.PuzzleContainer
				{
					Name = "Pack Container",
					Alignment = BoxContainer.AlignmentMode.Begin,
					Title = new RichTextLabel { Name = "Title", Text = pack.Name, FitContent = true }
						.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize),
					Container = new VBoxContainer { Name = "Packs", Alignment = BoxContainer.AlignmentMode.End }
						.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
				}
					.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);

				foreach (var puzzle in pack.Puzzles)
				{
					Button button = new() { Text = puzzle.Name };
					button.Pressed += () => Current.Puzzle = puzzle;
					container.Container.Add(button);
				}

				menu.PuzzleLoader.Control.Add(container);
			}
		}
	}
	private static DisplayContainer Init(this DisplayContainer container, Menu menu, params List<Display> displays)
	{
		Assert(displays.Count != 0, Errors.NoDisplayGiven);

		foreach (Display display in displays)
		{
			container.Tabs.Add(display);
			container.Add(display);
		}

		container.TabChanged += OnTabChanged;
		OnTabChanged(container.CurrentTab);

		return container;

		void OnTabChanged(long index)
		{
			Display current = Current.Display = container.CurrentTabDisplay;
			foreach (Display other in displays.ToList().Except([current]))
			{
				if (other == current
					|| other is not IHaveTools { Tools: PopupMenu otherTools }
					|| !menu.HasChild(otherTools)
				) continue;
				menu.RemoveChild(otherTools);
			}
			if (current is not IHaveTools { Tools: PopupMenu currentTools }
				|| menu.HasChild(currentTools)
			)
			{
				return;
			}
			menu.AddChild(currentTools);
		}
	}
}
