using Godot;

namespace RSG.UI;

using Nonogram;

public sealed partial class CoreUI : AspectRatioContainer
{
	public static CoreUI ConnectSignals(CoreUI container)
	{
		List<PuzzleSelector.PackDisplay> _levelSelectorDisplays = [];
		List<PuzzleSelector.PackDisplay> _levelLoaderDisplays = [];

		container.Menu.Buttons.Play.Pressed += container.Menu.Hide;
		container.Menu.Buttons.Levels.Pressed += container.Menu.Levels.Show;
		container.Menu.Buttons.Dialogues.Pressed += container.Menu.Dialogues.Show;
		container.Menu.Buttons.Settings.Pressed += container.Menu.Settings.Show;
		container.Menu.Buttons.Quit.Pressed += QuitPressed;

		container.Menu.Buttons.Settings.VisibilityChanged += SettingsVisibilityChanged;
		container.Menu.Levels.VisibilityChanged += FillPuzzleSelector;
		container.Menu.Dialogues.VisibilityChanged += FillDialogueSelector;

		container.Nonogram.ChildEnteredTree += OnChildEnteringTree;
		container.Nonogram.ChildExitingTree += OnChildExitingTree;
		container.Nonogram.Displays.TabChanged += OnDisplaysTabChanged;
		container.Nonogram.CompletionScreen.Levels.Pressed += OnLevelsPressed;

		container.Nonogram.ToolsBar.CodeLoader.Control.Input.TextChanged += OnCodeChanged;
		container.Nonogram.ToolsBar.CodeLoader.Control.Input.TextSubmitted += OnCodeSubmitted;
		container.Nonogram.ToolsBar.PuzzleLoader.AboutToPopup += LoadPuzzles;

		OnDisplaysTabChanged(container.Nonogram.Displays.CurrentTab);

		return container;

		void QuitPressed() => container.GetTree().Quit();
		void SettingsVisibilityChanged()
		{
			container.Menu.Buttons.Visible = !container.Menu.Settings.Visible;
		}
		void FillPuzzleSelector()
		{
			if (!container.Menu.Levels.Visible)
			{
				container.Menu.Hide();
				return;
			}
			foreach (PuzzleSelector.PackDisplay pack in _levelSelectorDisplays)
			{
				if (!IsInstanceValid(container.Menu.Levels.Puzzles.Value)) continue;
				if (container.Menu.Levels.Puzzles.Value.HasChild(pack))
				{
					container.Menu.Levels.Puzzles.Value.RemoveChild(pack);
					pack.QueueFree();
				}
			}
			IEnumerable<(string name, IEnumerable<Display.Data> data)> packConfigs = [
				("Saved Puzzles", PuzzleManager.GetSavedPuzzles()),
				.. PuzzleManager.GetPuzzlePacks().Select(PuzzleData.Pack.Convert)
			];

			foreach ((string name, IEnumerable<Display.Data> data) in packConfigs)
			{
				PuzzleSelector.PackDisplay display = PuzzleSelector.PackDisplay.Create(
					name: name,
					parent: container.Menu.Levels,
					data: data
				);
				container.Menu.Levels.Puzzles.Value.AddChild(display);
				_levelSelectorDisplays.Add(display);
			}
		}
		void FillDialogueSelector()
		{
			if (!container.Menu.Dialogues.Visible) return;
			container.Menu.Dialogues.Clear();
			container.Menu.Dialogues.Fill(Dialogues.AvailableDialogues);
		}
		void OnDisplaysTabChanged(long index)
		{
			Display current = container.Nonogram.Displays.CurrentTabDisplay;
			foreach (Display other in container.Nonogram.Displays.Tabs.ToList().Except([current]))
			{
				if (other == current
					|| other is not NonogramContainer.IHaveTools { Tools: PopupMenu otherTools }
					|| !container.Menu.HasChild(otherTools)
				) continue;
				container.Menu.RemoveChild(otherTools);
			}
			if (current is not NonogramContainer.IHaveTools { Tools: PopupMenu currentTools }
				|| container.Menu.HasChild(currentTools)
			)
			{
				return;
			}
			container.Menu.AddChild(currentTools);
		}
		void OnLevelsPressed()
		{
			container.Menu.Show();
			container.Menu.Levels.Show();
			container.Nonogram.CompletionScreen.Hide();
		}
		void OnCodeSubmitted(string value) => PuzzleManager.Load(value).Switch(
			container.Nonogram.Displays.CurrentTabDisplay.Load,
			error => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			notFound => GD.Print("Not Found")
		);
		void OnCodeChanged(string value) => PuzzleData.Code.Encode(value).Switch(
			error => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			code => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = $"valid code of size: {code.Size}"
		);
		void LoadPuzzles()
		{
			foreach (PuzzleSelector.PackDisplay pack in _levelLoaderDisplays)
			{
				if (!IsInstanceValid(container.Nonogram.ToolsBar.PuzzleLoader.Control)) continue;
				if (container.Nonogram.ToolsBar.PuzzleLoader.Control.HasChild(pack))
				{
					container.Nonogram.ToolsBar.PuzzleLoader.Control.RemoveChild(pack);
					pack.QueueFree();
				}
			}

			IEnumerable<(string name, IEnumerable<Display.Data> data)> packConfigs = [
				("Saved Puzzles", PuzzleManager.GetSavedPuzzles()),
				.. PuzzleManager.GetPuzzlePacks().Select(PuzzleData.Pack.Convert)
			];

			foreach ((string name, IEnumerable<Display.Data> data) in packConfigs)
			{
				PuzzleSelector.PackDisplay display = PuzzleSelector.PackDisplay.Create(
					name: name,
					parent: container.Nonogram.ToolsBar,
					data: data
				);
				container.Nonogram.ToolsBar.PuzzleLoader.Control.AddChild(display);
				_levelLoaderDisplays.Add(display);
			}
		}
		void OnChildEnteringTree(Node node)
		{
			switch (node)
			{
				case NonogramContainer.GameDisplay display:
					container.Nonogram.Status.CompletionLabel.Visible = true;
					break;
				case Display: break;
				case Node when container.Nonogram.Displays.HasChild(node):
					GD.PushWarning($"Child Added is not of type {typeof(Display)}, removing child {nameof(node)}");
					container.Nonogram.Displays.RemoveChild(node);
					break;
			}
		}
		void OnChildExitingTree(Node node)
		{
			switch (node)
			{
				case NonogramContainer.GameDisplay display:
					container.Nonogram.Status.CompletionLabel.Visible = false;
					break;
			}
		}
	}

	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public NonogramContainer Nonogram { get; } = PuzzleManager.Current.UI;
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", Colours = Core.Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public ColourPack Colours { set { Menu.Background.Color = value.NonogramBackground; } }

	public override void _Ready() => this.Add(PuzzleManager.Current.UI, Menu, Dialogues.Container, LoadingScreen);
	public void StepBack() => Menu.StepBack(Menu.Settings, Menu.Levels, Menu.Dialogues);
}

