using Godot;
using RSG.Nonogram;

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
			AnchorLeft = .1f,
			VerticalAlignment = VerticalAlignment.Center,
			MouseFilter = MouseFilterEnum.Ignore
		}.Preset(LayoutPreset.FullRect);
		public MainButton(string name)
		{
			const int fontSize = 20;
			Text = Name = name.AddSpacesToPascalCase();
			Label.PushFontSize(fontSize);
			Label.PushColor(Colors.Black);
			Label.AddText(Text);
			Resized += () => Background.TextureNoise((Vector2I)Size, colour: Colour);
		}
		public override void _Ready()
		{
			this.Add(Background, Label)
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Fill)
				.AddAllFontThemeOverride(Colors.Transparent)
				.OverrideStyle(modify: (StyleBoxFlat style) =>
				{
					style.CornerDetail = 1;
					style.BgColor = Colors.Transparent;
					style.SetContentMarginAll(10);
					return style;
				});
			Label
				.OverrideStyle(modify: (StyleBoxFlat style) =>
				{
					style.CornerDetail = 1;
					return style;
				})
				.AddThemeFontSizeOverride("normal", 40);

		}
		static Color Colour(float value)
		{
			Color color = Colors.OliveDrab;
			return value switch
			{
				< .1f => color.Darkened(.2f),
				> .4f => color with { A = .4f },
				_ => color with { A = .7f }
			};
		}

	}
	public sealed partial class MainButtons : HBoxContainer
	{
		public BaseButton Play { get; } = new MainButton(nameof(Play))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public BaseButton PlayMinesweeper { get; } = new MainButton(nameof(PlayMinesweeper))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public BaseButton Levels { get; } = new MainButton(nameof(Levels))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public BaseButton Dialogues { get; } = new MainButton(nameof(Dialogues))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public BaseButton Settings { get; } = new MainButton(nameof(Settings))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public BaseButton Quit { get; } = new MainButton(nameof(Quit))
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
		public VBoxContainer Container { get; } = new VBoxContainer { Name = "Container", Alignment = AlignmentMode.End }
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		public Container Spacer { get; } = new BoxContainer { Name = "Spacer", SizeFlagsStretchRatio = 2f }
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		public override void _Ready() => this.Add(
				Container.Add(Play, PlayMinesweeper, Levels, Dialogues, Settings, Quit),
				Spacer
			);
	}
	public interface IPress
	{
		void PlayPressed();
		void PlayMinesweeperPressed();
		void LevelsPressed();
		void DialoguesPressed();
		void SettingsPressed();
		void QuitPressed();
	}
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
	public SettingsContainer Settings { get; } = new SettingsContainer { Name = "Settings", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, Margin);
	public MainButtons Buttons { get; } = new MainButtons { Name = nameof(Buttons), }
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

	private IReceiveSignals Signals
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
	private IPress OnPressed
	{
		set
		{
			Buttons.Play.Pressed += value.PlayPressed;
			Buttons.PlayMinesweeper.Pressed += value.PlayMinesweeperPressed;
			Buttons.Levels.Pressed += value.LevelsPressed;
			Buttons.Dialogues.Pressed += value.DialoguesPressed;
			Buttons.Settings.Pressed += value.SettingsPressed;
			Buttons.Quit.Pressed += value.QuitPressed;
			if (field is null)
			{
				field = value;
				return;
			}
			Buttons.Play.Pressed -= field.PlayPressed;
			Buttons.PlayMinesweeper.Pressed -= field.PlayMinesweeperPressed;
			Buttons.Levels.Pressed -= field.LevelsPressed;
			Buttons.Dialogues.Pressed -= field.DialoguesPressed;
			Buttons.Settings.Pressed -= field.SettingsPressed;
			Buttons.Quit.Pressed -= field.QuitPressed;
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
	public void OverrideSignals<T>(T value)
	where T : IPress, IReceiveSignals, SettingsMenuContainer.IChangeSettings
	{
		Signals = value;
		OnPressed = value;
		Settings.Nonogram.SettingsChanger = value;
	}
}