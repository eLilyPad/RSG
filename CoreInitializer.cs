using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using static Nonogram.PuzzleManager;

public static class CoreInitializer
{
	private static class Errors
	{
		public const string NoDisplayGiven = "No Displays given, provide at least one display when creating the Container";
		public const string OutsideOfTree = "Outside of Scene Tree unable to initialize";
	}

	public static PuzzleSelector Init(this PuzzleSelector container, MainMenu menu)
	{
		foreach (PuzzleData.Pack pack in GetPuzzlePacks())
		{
			PuzzleSelector.PackDisplay display = new PuzzleSelector.PackDisplay { Name = pack.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
			foreach (PuzzleData puzzle in pack.Puzzles)
			{
				PuzzleSelector.PuzzleDisplay puzzleDisplay = new()
				{
					Name = puzzle.Name + " Display",
					Button = new() { Name = puzzle.Name + " Button", Text = puzzle.Name },
					Ratio = 1f
				};
				puzzleDisplay.Button.Pressed += Pressed;
				display.Puzzles.Value.Add(puzzleDisplay);

				void Pressed()
				{
					Current.Puzzle = puzzle;
					container.Hide();
					menu.Hide();
				}
			}
			container.PuzzlePacks.Value.Add(display);
		}
		return container;
	}
	public static MainMenu Init(
		this MainMenu menu, PuzzleSelector levels, ColourPack colours
	)
	{
		Assert(condition: menu.IsInsideTree(), $"{nameof(MainMenu)}- {Errors.OutsideOfTree}");

		menu.Background.Color = colours.NonogramBackground;

		menu.Buttons.Play.Pressed += PlayPressed;
		menu.Buttons.Levels.Pressed += LevelPressed;
		menu.Buttons.Settings.Pressed += SettingsPressed;
		menu.Buttons.Quit.Pressed += QuitPressed;

		menu.Settings.VisibilityChanged += () => menu.Buttons.Visible = !menu.Settings.Visible;

		return menu;

		void PlayPressed() => menu.Hide();
		void LevelPressed() => levels.Show();
		void SettingsPressed() => menu.Settings.Show();
		void QuitPressed() => menu.GetTree().Quit();
	}
	public static NonogramContainer Init(this NonogramContainer container)
	{
		Assert(condition: container.IsInsideTree(), $"{nameof(NonogramContainer)}- {Errors.OutsideOfTree}");

		Display.Data startPuzzle = Current.Puzzle;

		container.ChildEnteredTree += OnChildEnteringTree;
		container.ChildExitingTree += OnChildExitingTree;

		container.Displays.Init(
			menu: container.ToolsBar,
			new NonogramContainer.GameDisplay { Name = "Game", Status = container.Status },
			new NonogramContainer.PaintDisplay { Name = "Paint", },
			new NonogramContainer.ExpectedDisplay { Name = "Expected", }
		);
		container.ToolsBar.Init(container.Displays);

		container.Displays.CurrentTabDisplay.Load(Current.Puzzle);

		return container;

		void OnChildEnteringTree(Node node)
		{
			switch (node)
			{
				case NonogramContainer.GameDisplay display:
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
				case NonogramContainer.GameDisplay display:
					container.Status.CompletionLabel.Visible = false;
					break;
			}
		}
	}

	private static Menu Init(
		this Menu menu, NonogramContainer.DisplayContainer displays
	)
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
	private static NonogramContainer.DisplayContainer Init(
		this NonogramContainer.DisplayContainer container, Menu menu, params IEnumerable<Display> displays
	)
	{
		Assert(displays.Any(), Errors.NoDisplayGiven);

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
					|| other is not NonogramContainer.IHaveTools { Tools: PopupMenu otherTools }
					|| !menu.HasChild(otherTools)
				) continue;
				menu.RemoveChild(otherTools);
			}
			if (current is not NonogramContainer.IHaveTools { Tools: PopupMenu currentTools }
				|| menu.HasChild(currentTools)
			)
			{
				return;
			}
			menu.AddChild(currentTools);
		}
	}
}
