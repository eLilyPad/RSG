using Godot;

namespace RSG.UI;

using Nonogram;
using RSG.Console;
using RSG.Dialogue;

public sealed partial class CoreUI : Control
{
	private sealed class UIEventHandler(CoreUI UI) : PuzzleCompleteScreen.IHandleSignals
	{
		void PuzzleCompleteScreen.IHandleSignals.OnLevelsPressed()
		{
			UI.Menu.Show();
			UI.Menu.Levels.Show();
			PuzzleManager.Current.UI.CompletionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnDialoguesPressed()
		{
			UI.Menu.Show();
			UI.Menu.Dialogues.Show();
			PuzzleManager.Current.UI.CompletionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnPlayDialoguePressed()
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			Dialogues.Start(name: current.CompletionDialogueName);
			UI.Menu.Show();
			UI.Menu.Buttons.Show();
			current.UI.CompletionScreen.Hide();
		}
		void PuzzleCompleteScreen.IHandleSignals.OnVisibilityChanged()
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			PuzzleCompleteScreen completionScreen = current.UI.CompletionScreen.Value;
			string name = current.CompletionDialogueName;
			bool hasDialogue = Dialogues.Contains(name);
			completionScreen.Options.PlayDialogue.Visible = hasDialogue;
			if (hasDialogue)
			{
				completionScreen.Report.Value.Log.Text = "Dialogue: " + name;
			}
		}
	}

	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);

	public AnimatedFigure MainMenuFigure
	{
		get
		{
			if (field is not null) return field;
			AnimatedFigure animatedFigure = new()
			{
				Name = "Figure",
				Position = GetTree().CurrentScene.GetViewport().GetVisibleRect().GetCenter()
			};
			AddChild(animatedFigure);
			return field = animatedFigure;
		}
	}

	public required ColourPack Colours { set => PuzzleManager.Current.UI.Colours = Menu.Colours = value; }

	private UIEventHandler Handler => field ??= new(UI: this);
	public override void _Ready()
	{
		NonogramContainer nonogram = PuzzleManager.Current.UI;
		this.Add(
			nonogram,
			Dialogues.Container,
			Console.Container,
			Menu,
			LoadingScreen
		);
		nonogram.CompletionScreen.Value.Signals = Handler;
	}

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

