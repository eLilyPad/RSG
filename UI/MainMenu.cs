using Godot;

namespace RSG.UI;

public sealed partial class MainMenu : Container
{
	public interface IReceiveSignals
	{
		void MenuVisibilityChanged();
		void PuzzleSelectorVisibilityChanged();
		void DialogueSelectorVisibilityChanged();
	}

	public const int Margin = 100;
	public ColourPack Colours
	{
		set
		{
			Levels.Background.Color = value.MainMenuDialoguesBackground;
			Dialogues.Background.Color = value.MainMenuDialoguesBackground;
			Background.Color = value.MainMenuBackground with { A = .3f };
		}
	}
	public ColorRect Background { get; } = new ColorRect { Name = nameof(Background) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenuSettings Settings { get; } = new MainMenuSettings { Name = "Settings", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public MainMenuButtons Buttons { get; } = new MainMenuButtons { Name = nameof(Buttons), }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin / 2);
	public Nonogram.PuzzleSelector Levels { get; } = new Nonogram.PuzzleSelector
	{
		Name = "Level Selector",
		Visible = false
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public DialogueSelector Dialogues { get; } = new DialogueSelector
	{
		Name = "Dialogue Selector",
		Visible = false
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public StudioContainer Studio { get; } = new StudioContainer { Name = "Studio" };

	public IReceiveSignals Signals
	{
		set
		{
			VisibilityChanged += value.MenuVisibilityChanged;
			Levels.VisibilityChanged += value.PuzzleSelectorVisibilityChanged;
			Dialogues.VisibilityChanged += value.DialogueSelectorVisibilityChanged;
			if (field is null)
			{
				field = value;
				return;
			}
			VisibilityChanged -= field.MenuVisibilityChanged;
			Levels.VisibilityChanged -= field.PuzzleSelectorVisibilityChanged;
			Dialogues.VisibilityChanged -= field.DialogueSelectorVisibilityChanged;
		}
	}

	public MainMenu()
	{
		VisibilityChanged += () =>
		{
			if (!Visible) return;
			IEnumerable<Node> children = GetChildren();
			IEnumerable<Node> visibleChildren = children
				.Where(n => n is Control control && control.Visible);

			switch (visibleChildren.Count())
			{
				case 0:
					Buttons.Show();
					Background.Show();
					break;
				case 1 when visibleChildren.First() == Background:
					Buttons.Show();
					break;
				case 1 when visibleChildren.First() == Buttons:
					Background.Show();
					break;
				default: break;
			}

		};
		Settings.VisibilityChanged += () => Buttons.Visible = !Settings.Visible;
		Dialogues.VisibilityChanged += () => Buttons.Visible = !Dialogues.Visible;
		Levels.VisibilityChanged += () => Buttons.Visible = !Levels.Visible;
	}

	public override void _Ready() => this.Add(Background, Buttons, Settings, Levels, Dialogues);
}