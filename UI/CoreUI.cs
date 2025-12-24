using Godot;

namespace RSG.UI;

using Nonogram;
using static DialogueSelector;
using static Nonogram.PuzzleSelector;

public sealed partial class CoreUI : AspectRatioContainer
{
	public static CoreUI ConnectSignals(CoreUI container)
	{
		List<PackDisplay> levelSelectorDisplays = [];
		List<PackDisplay> levelLoaderDisplays = [];
		List<DialogueDisplay> dialogueLoaderDisplays = [];

		ConsoleContainer console = Console.Container;
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		NonogramContainer.PuzzleCompleteScreen completionScreen = nonogram.CompletionScreen;
		PuzzleSelector puzzleSelector = container.Menu.Levels;
		DialogueSelector dialogueSelector = container.Menu.Dialogues;

		container.Menu.Buttons.Play.Pressed += container.Menu.Hide;
		container.Menu.Buttons.Levels.Pressed += container.Menu.Levels.Show;
		container.Menu.Buttons.Dialogues.Pressed += container.Menu.Dialogues.Show;
		container.Menu.Buttons.Settings.Pressed += container.Menu.Settings.Show;
		container.Menu.Buttons.Quit.Pressed += () => container.GetTree().Quit();
		container.Menu.Settings.VisibilityChanged += () => container.Menu.Buttons.Visible = !container.Menu.Settings.Visible;
		puzzleSelector.VisibilityChanged += () =>
		{
			if (!puzzleSelector.Visible)
			{
				container.Menu.Hide();
				return;
			}
			Refill(
				root: puzzleSelector,
				parent: puzzleSelector.Puzzles.Value,
				nodes: levelSelectorDisplays,
				configs: PuzzleManager.SelectorConfigs,
				create: PackDisplay.Create
			);
		};
		dialogueSelector.VisibilityChanged += () => Refill(
			root: dialogueSelector,
			parent: dialogueSelector.DisplayContainer.Value,
			nodes: dialogueLoaderDisplays,
			configs: Dialogues.AvailableDialogues,
			create: DialogueDisplay.Create
		);
		nonogram.ChildEnteredTree += node =>
		{
			switch (node)
			{
				case NonogramContainer.GameDisplay display:
					nonogram.Status.CompletionLabel.Visible = true;
					break;
				case Display: break;
				case Node when nonogram.Displays.HasChild(node):
					GD.PushWarning($"Child Added is not of type {typeof(Display)}, removing child {nameof(node)}");
					nonogram.Displays.RemoveChild(node);
					break;
			}
		};
		nonogram.ChildExitingTree += node => nonogram.Status.CompletionLabel.Visible = node is not NonogramContainer.GameDisplay;
		nonogram.Displays.TabChanged += _ => PuzzleManager.Current.OnDisplayTabChanged(container.Menu);
		completionScreen.Levels.Pressed += () => completionScreen.Visible = !(puzzleSelector.Visible = true);
		nonogram.ToolsBar.CodeLoader.Control.Input.TextChanged += PuzzleManager.Current.WhenCodeLoaderEdited;
		nonogram.ToolsBar.CodeLoader.Control.Input.TextSubmitted += PuzzleManager.Current.WhenCodeLoaderEntered;
		nonogram.ToolsBar.PuzzleLoader.AboutToPopup += () => Refill(
			root: nonogram.ToolsBar,
			parent: nonogram.ToolsBar.PuzzleLoader.Control,
			nodes: levelLoaderDisplays,
			configs: PuzzleManager.SelectorConfigs,
			create: PackDisplay.Create
		);

		Console.Container.Input.Line.VisibilityChanged += () => Console.GrabInputFocus(true);
		Console.Container.Input.Line.TextSubmitted += input =>
		{
			if (input.Length == 0) return;
			Console.Instance.Submitted(input);
			Console.Container.Log.Label.Text += input + "\n";
			Console.GrabInputFocus(clearSuggestions: true);
		};
		Console.Container.Input.Line.TextChanged += input =>
		{
			Console.Container.Input.SuggestionDisplay.Clear();
			IEnumerable<string> suggestions = Console.Instance.Suggestions(input);
			foreach (string suggestion in suggestions)
			{
				Console.Container.Input.SuggestionDisplay.AddItem(suggestion);
			}
		};
		Console.Container.Input.SuggestionDisplay.ItemSelected += index =>
		{
			string suggestion = Console.Container.Input.SuggestionDisplay.GetItemText((int)index);
			if (!Console.Container.Input.Line.Text.EndsWith(' '))
			{
				Console.Container.Input.Line.Text += ' ';
			}
			Console.Container.Input.Line.Text += suggestion;
			Console.GrabInputFocus();
		};

		PuzzleManager.Current.OnDisplayTabChanged(container.Menu);

		return container;

		static void Refill<TConfig, TParent, TNode>(
			CanvasItem root,
			TParent parent,
			List<TNode> nodes,
			IEnumerable<TConfig> configs,
			Func<TConfig, CanvasItem, TNode> create
		)
		where TParent : Node
		where TNode : Node
		{
			if (root is { Visible: false }) return;
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
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", Colours = Core.Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public override void _Ready() => this.Add(
		PuzzleManager.Current.UI,
		Menu,
		Dialogues.Container,
		Console.Container,
		LoadingScreen
	);
	public void StepBack()
	{
		Menu.StepBack(Console.Container, Menu.Settings, Menu.Levels, Menu.Dialogues);
	}

	public static void ToggleConsole() => Console.Container.Visible = !Console.Container.Visible;
}

