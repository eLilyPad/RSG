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
		container.Menu.Buttons.Quit.Pressed += () => container.GetTree().Quit();
		container.Menu.Settings.VisibilityChanged += () => container.Menu.Buttons.Visible = !container.Menu.Settings.Visible;
		container.Menu.Levels.VisibilityChanged += () =>
		{
			if (!container.Menu.Levels.Visible)
			{
				container.Menu.Hide();
				return;
			}
			container.Menu.Levels.Puzzles.Value.Remove(free: true, _levelSelectorDisplays);
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
		};
		container.Menu.Dialogues.VisibilityChanged += () =>
		{
			if (!container.Menu.Dialogues.Visible) return;
			container.Menu.Dialogues.Clear();
			container.Menu.Dialogues.Fill(Dialogues.AvailableDialogues);
		};

		container.Nonogram.ChildEnteredTree += node =>
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
		};
		container.Nonogram.ChildExitingTree += node =>
		{
			switch (node)
			{
				case NonogramContainer.GameDisplay display:
					container.Nonogram.Status.CompletionLabel.Visible = false;
					break;
			}
		};
		container.Nonogram.Displays.TabChanged += OnDisplaysTabChanged;
		container.Nonogram.CompletionScreen.Levels.Pressed += () =>
		{
			container.Menu.Show();
			container.Menu.Levels.Show();
			container.Nonogram.CompletionScreen.Hide();
		};
		container.Nonogram.ToolsBar.CodeLoader.Control.Input.TextChanged += value => PuzzleData.Code.Encode(value).Switch(
			error => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			code => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = $"valid code of size: {code.Size}"
		);
		container.Nonogram.ToolsBar.CodeLoader.Control.Input.TextSubmitted += value => PuzzleManager.Load(value).Switch(
			container.Nonogram.Displays.CurrentTabDisplay.Load,
			error => container.Nonogram.ToolsBar.CodeLoader.Control.Validation.Text = error.Message,
			notFound => GD.Print("Not Found")
		);
		container.Nonogram.ToolsBar.PuzzleLoader.AboutToPopup += () =>
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
		};

		container.Console.Input.Line.TextSubmitted += input =>
		{
			if (input.Length == 0) return;
			RSG.Console.Instance.Submitted(input);
			container.Console.Log.Label.Text += input + "\n";
			GrabConsoleInputFocus(clear: true);
		};
		container.Console.Input.Line.TextChanged += input =>
		{
			container.Console.Input.SuggestionDisplay.Clear();
			IEnumerable<string> suggestions = RSG.Console.Instance.Suggestions(input);
			foreach (string suggestion in suggestions)
			{
				container.Console.Input.SuggestionDisplay.AddItem(suggestion);
			}
		};
		container.Console.Input.Line.VisibilityChanged += () => GrabConsoleInputFocus(true);
		container.Console.Input.SuggestionDisplay.ItemSelected += index =>
		{
			string suggestion = container.Console.Input.SuggestionDisplay.GetItemText((int)index);
			if (!container.Console.Input.Line.Text.EndsWith(' '))
			{
				container.Console.Input.Line.Text += ' ';
			}
			container.Console.Input.Line.Text += suggestion;
			GrabConsoleInputFocus();
		};

		OnDisplaysTabChanged(container.Nonogram.Displays.CurrentTab);

		return container;

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
		void GrabConsoleInputFocus(bool clear = false)
		{
			if (clear)
			{
				container.Console.Input.Line.Clear();
			}
			if (!container.Console.Input.Line.IsVisibleInTree()) return;
			container.Console.Input.Line.GrabFocus();
		}
	}

	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public NonogramContainer Nonogram { get; } = PuzzleManager.Current.UI;
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", Colours = Core.Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ConsoleContainer Console { get; } = new ConsoleContainer
	{
		Name = "Console",
		Visible = false,
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public ColourPack Colours { set { Menu.Background.Color = value.NonogramBackground; } }

	public override void _Ready() => this.Add(
		PuzzleManager.Current.UI,
		Menu,
		Dialogues.Container,
		Console,
		LoadingScreen
	);
	public void StepBack()
	{
		Menu.StepBack(Console, Menu.Settings, Menu.Levels, Menu.Dialogues);
	}

	public void ToggleConsole() => Console.Visible = !Console.Visible;
}

