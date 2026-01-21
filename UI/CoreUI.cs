using Godot;

namespace RSG.UI;

using Nonogram;
using RSG.Console;
using RSG.Dialogue;
using static DialogueSelector;
using static Nonogram.PuzzleSelector;

public sealed partial class CoreUI : Control
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
		List<PackDisplay> levelLoaderDisplays = [];

		ConsoleContainer console = Console.Container;
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		PuzzleCompleteScreen completionScreen = nonogram.CompletionScreen.Value;
		PuzzleSelector puzzleSelector = container.Menu.Levels;
		DialogueSelector dialogueSelector = container.Menu.Dialogues;

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
			nonogram.CompletionScreen.ReplaceVisibility(container.Menu.Buttons);
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

		return container;
	}

	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);

	public required ColourPack Colours { set => PuzzleManager.Current.UI.Colours = Menu.Colours = value; }

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

