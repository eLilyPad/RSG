using Godot;

namespace RSG.UI;

using Nonogram;
using static DialogueSelector;
using static Nonogram.PuzzleSelector;

public sealed partial class CoreUI : PanelContainer
{
	public static CoreUI SetThemes(CoreUI container)
	{
		ConsoleContainer console = Console.Container;
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		PuzzleCompleteScreen completionScreen = nonogram.CompletionScreen.Value;
		PuzzleSelector puzzleSelector = container.Menu.Levels;
		DialogueSelector dialogueSelector = container.Menu.Dialogues;

		//completionScreen.AddThemeStyleboxOverride(dialogueSelector);
		//int marginValue = 200;
		//completionScreen.AddThemeConstantOverride("margin_top", marginValue);
		//completionScreen.AddThemeConstantOverride("margin_left", marginValue);
		//completionScreen.AddThemeConstantOverride("margin_bottom", marginValue);
		//completionScreen.AddThemeConstantOverride("margin_right", marginValue);

		return container;
	}
	public static CoreUI ConnectSignals(CoreUI container)
	{
		List<PackDisplay> levelSelectorDisplays = [];
		List<PackDisplay> levelLoaderDisplays = [];
		List<DialogueDisplay> dialogueLoaderDisplays = [];

		ConsoleContainer console = Console.Container;
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		PuzzleCompleteScreen completionScreen = nonogram.CompletionScreen.Value;
		PuzzleSelector puzzleSelector = container.Menu.Levels;
		DialogueSelector dialogueSelector = container.Menu.Dialogues;

		SettingsMenuContainer.ConnectSignals(menu: container.Menu.Settings.Nonogram, current: PuzzleManager.Current);

		puzzleSelector.VisibilityChanged += () =>
		{
			if (!puzzleSelector.Visible)
			{
				container.Menu.Hide();
				return;
			}
			RefillPacks(
				root: puzzleSelector,
				parent: puzzleSelector.Puzzles.Value,
				nodes: levelSelectorDisplays
			);
		};
		dialogueSelector.VisibilityChanged += () => Refill(
			root: dialogueSelector,
			parent: dialogueSelector.DisplayContainer.Value,
			nodes: dialogueLoaderDisplays,
			configs: Dialogues.AvailableDialogues,
			create: DialogueDisplay.Create
		);
		completionScreen.Options.Levels.Pressed += () =>
		{
			nonogram.CompletionScreen.ReplaceVisibility(container.Menu, puzzleSelector);
			nonogram.Hide();
		};
		completionScreen.Options.Dialogues.Pressed += () =>
		{
			nonogram.CompletionScreen.ReplaceVisibility(container.Menu, dialogueSelector);
			nonogram.Hide();
		};
		completionScreen.Options.PlayDialogue.Pressed += () =>
		{
			Dialogues.Start(name: PuzzleManager.Current.CompletionDialogueName);
			nonogram.CompletionScreen.ReplaceVisibility(container.Menu, container.Menu.Buttons);
			nonogram.Hide();
		};
		completionScreen.VisibilityChanged += () =>
		{
			string name = PuzzleManager.Current.CompletionDialogueName;
			bool hasDialogue = Dialogues.Contains(name);
			completionScreen.Options.PlayDialogue.Visible = hasDialogue;
			if (hasDialogue)
			{
				completionScreen.Report.Value.Log.Text = "Dialogue: " + name;
			}
		};
		nonogram.ToolsBar.PuzzleLoader.AboutToPopup += () => RefillPacks(
			root: nonogram.ToolsBar,
			parent: nonogram.ToolsBar.PuzzleLoader.Control,
			nodes: levelLoaderDisplays
		);

		console.Input.Line.VisibilityChanged += () => Console.GrabInputFocus(true);
		console.Input.Line.TextSubmitted += Console.Instance.Submitted;
		console.Input.Line.TextChanged += input =>
		{
			console.Input.SuggestionDisplay.Clear();
			IEnumerable<string> suggestions = Console.Instance.Suggestions(input);
			foreach (string suggestion in suggestions)
			{
				console.Input.SuggestionDisplay.AddItem(suggestion);
			}
		};
		console.Input.SuggestionDisplay.ItemSelected += index =>
		{
			string suggestion = console.Input.SuggestionDisplay.GetItemText((int)index);
			if (!console.Input.Line.Text.EndsWith(' '))
			{
				console.Input.Line.Text += ' ';
			}
			console.Input.Line.Text += suggestion;
			Console.GrabInputFocus();
		};

		nonogram.Resized += () => nonogram.Background.Border.TextureBorder((Vector2I)nonogram.Size);

		return container;
		static void RefillPacks(CanvasItem root, Node parent, List<PackDisplay> nodes)
		{
			Refill(root, parent, nodes, configs: PuzzleManager.SelectorConfigs, create: PackDisplay.Create);
		}
		static void Refill<TConfig, TNode>(
			CanvasItem root,
			Node parent,
			List<TNode> nodes,
			IEnumerable<TConfig> configs,
			Func<TConfig, CanvasItem, TNode> create
		)
		where TNode : Node
		{
			if (!root.Visible) return;
			parent.Remove(true, nodes);
			nodes.Clear();
			foreach (TConfig config in configs)
			{
				TNode node = create(config, root);
				parent.AddChild(node);
				nodes.Add(node);
			}
		}
	}

	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public required ColourPack Colours
	{
		set
		{
			PuzzleManager.Current.UI.Colours = value;
			Menu.Colours = value;
		}
	}

	public override void _Ready() => this.Add(
		PuzzleManager.Current.UI,
		Dialogues.Container,
		Console.Container,
		Menu,
		LoadingScreen
	);
	public void EscapePressed()
	{
		if (!Menu.Visible)
		{
			Menu.Show();
			Menu.Buttons.Show();
			return;
		}
		ReadOnlySpan<Control> steps = [
			Console.Container,
			PuzzleManager.Current.UI.CompletionScreen,
			Menu.Settings,
			Menu.Levels,
			Menu.Dialogues
		];
		foreach (Control control in steps)
		{
			if (control.Visible)
			{
				control.Hide();
				Menu.Show();
				Menu.Buttons.Show();
				return;
			}
		}
	}

	public static void ToggleConsole() => Console.Container.Visible = !Console.Container.Visible;
}

