using Godot;

namespace RSG.UI;

public sealed partial class MainMenu : Container
{
	public sealed partial class SettingsContainer : TabContainer
	{
		public const int Margin = 0;
		public Audio.Container Audio { get; } = new Audio.Container { Name = "Audio" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public Video.Container Video { get; } = new Video.Container { Name = "Video" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public Input.Container Input { get; } = new Input.Container { Name = "Input" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public Nonogram.SettingsMenuContainer Nonogram { get; } = new Nonogram.SettingsMenuContainer { Name = "Nonogram" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public override void _Ready() => this.Add(Audio, Video, Input, Nonogram);
	}
	public sealed partial class MainButtons : VBoxContainer
	{
		public Button Play { get; } = new() { Name = nameof(Play), Text = nameof(Play) };
		public Button Levels { get; } = new() { Name = nameof(Levels), Text = nameof(Levels) };
		public Button Dialogues { get; } = new() { Name = nameof(Dialogues), Text = nameof(Dialogues) };
		public Button Settings { get; } = new() { Name = nameof(Settings), Text = nameof(Settings) };
		public Button Quit { get; } = new() { Name = nameof(Quit), Text = nameof(Quit) };
		public override void _Ready() => this.Add(Play, Levels, Dialogues, Settings, Quit);
	}

	public const int Margin = 100;
	public required ColourPack Colours { get; init => Background.Color = (field = value).MainMenuBackground; }
	public ColorRect Background { get; } = new ColorRect { Name = nameof(Background) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public SettingsContainer Settings { get; } = new SettingsContainer { Name = "Settings", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public MainButtons Buttons { get; } = new MainButtons { Name = nameof(Buttons) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public Nonogram.PuzzleSelector Levels { get; } = new Nonogram.PuzzleSelector { Name = "Level Selector", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public DialogueSelector Dialogues { get; } = new DialogueSelector { Name = "Dialogue Selector", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);

	public MainMenu()
	{
		Buttons.Play.Pressed += Hide;
		Buttons.Levels.Pressed += Levels.Show;
		Buttons.Dialogues.Pressed += Dialogues.Show;
		Buttons.Settings.Pressed += Settings.Show;
		Buttons.Quit.Pressed += () => GetTree().Quit();
		Settings.VisibilityChanged += () => Buttons.Visible = !Settings.Visible;
	}

	public override void _Ready() => this.Add(Background, Buttons, Settings, Levels, Dialogues);
}