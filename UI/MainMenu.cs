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
	private sealed partial class MainButton : Button
	{
		public TextureRect Background { get; } = new TextureRect { Name = "Background", }
			.Preset(LayoutPreset.FullRect);
		public RichTextLabel Label { get; } = new RichTextLabel
		{
			Name = "Label",
			SelectionEnabled = false,
			FitContent = true,
			BbcodeEnabled = true,
			MouseFilter = MouseFilterEnum.Ignore
		}.Preset(LayoutPreset.FullRect);
		public MainButton(string name)
		{
			Name = name;
			Label.PushColor(Colors.Black);
			Label.AddText(name);
			Text = name;
		}
		public override void _Ready()
		{
			this
				.Add(Background, Label)
				.SizeFlags(SizeFlags.Fill, SizeFlags.Fill)
				.AddAllFontThemeOverride(Colors.Transparent)
				.OverrideStyle(modify: (StyleBoxFlat style) =>
				{
					style.CornerDetail = 1;
					style.BgColor = Colors.Transparent;
					//style.ExpandMarginRight = 100;
					return style;
				});
			Label
				.OverrideStyle(modify: (StyleBoxFlat style) =>
				{
					style.CornerDetail = 1;
					//style.BgColor = Colors.Transparent;
					//style.ExpandMarginRight = 100;
					return style;
				})
				.AddThemeFontSizeOverride("normal", 40);
			Resized += () => Background.TextureNoise((Vector2I)Size, value => value switch
			{
				< .1f => Colors.DarkGray,
				> .6f => Colors.DarkSlateGray,
				_ => Colors.Transparent
			});
		}
	}
	public sealed partial class MainButtons : VBoxContainer
	{
		public BaseButton Play { get; } = new MainButton(nameof(Play));
		public BaseButton Levels { get; } = new MainButton(nameof(Levels));
		public BaseButton Dialogues { get; } = new MainButton(nameof(Dialogues));
		public BaseButton Settings { get; } = new MainButton(nameof(Settings));
		public BaseButton Quit { get; } = new MainButton(nameof(Quit));
		public override void _Ready() => this.Add(Play, Levels, Dialogues, Settings, Quit);
	}

	public const int Margin = 100;
	public required ColourPack Colours { get; init => Background.Color = (field = value).MainMenuBackground; }
	public ColorRect Background { get; } = new ColorRect { Name = nameof(Background) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public SettingsContainer Settings { get; } = new SettingsContainer { Name = "Settings", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public MainButtons Buttons { get; } = new MainButtons { Name = nameof(Buttons) }
		//.SizeFlags(SizeFlags.Fill, SizeFlags.Fill)
		.Preset(preset: LayoutPreset.BottomLeft, resizeMode: LayoutPresetMode.KeepSize, Margin);
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