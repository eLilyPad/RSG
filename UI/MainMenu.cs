using Godot;

namespace RSG.UI;

[AddOnNodeReady]
public sealed partial class MainMenu : Container
{
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
	public IPress OnPressed
	{
		set
		{
			BaseButton
				play = Buttons.Container.Play,
				playMinesweeper = Buttons.Container.PlayMinesweeper,
				studio = Buttons.Container.Studio,
				levels = Buttons.Container.Levels,
				dialogues = Buttons.Container.Dialogues,
				settings = Buttons.Container.Settings,
				quit = Buttons.Container.Quit;
			play.Pressed += value.PlayPressed;
			playMinesweeper.Pressed += value.PlayMinesweeperPressed;
			studio.Pressed += value.OpenStudioPressed;
			levels.Pressed += value.LevelsPressed;
			dialogues.Pressed += value.DialoguesPressed;
			settings.Pressed += value.SettingsPressed;
			quit.Pressed += value.QuitPressed;
			if (field is null)
			{
				field = value;
				return;
			}
			studio.Pressed -= field.OpenStudioPressed;
			play.Pressed -= field.PlayPressed;
			playMinesweeper.Pressed -= field.PlayMinesweeperPressed;
			levels.Pressed -= field.LevelsPressed;
			dialogues.Pressed -= field.DialoguesPressed;
			settings.Pressed -= field.SettingsPressed;
			quit.Pressed -= field.QuitPressed;
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
	[AddOnNodeReady]
	public sealed partial class SettingsContainer : TabContainer
	{
		public Audio.Container Audio { get; } = new Audio.Container { Name = "Audio" }
			.SizeFlags(both: SizeFlags.Fill);
		public Video.Container Video { get; } = new Video.Container { Name = "Video" }
			.SizeFlags(both: SizeFlags.Fill);
		public Input.Container Input { get; } = new Input.Container { Name = "Input" }
			.SizeFlags(both: SizeFlags.Fill);
		public Nonogram.SettingsMenuContainer Nonogram { get; } = new Nonogram.SettingsMenuContainer { Name = "Nonogram" }
			.SizeFlags(both: SizeFlags.Fill);
		private static MarginContainer Marginalize(Node node)
		{
			const int marginValue = 60;
			var backgroundColour = Colors.OliveDrab with { A = .8f };
			var background = new ColorRect { Name = "Background", Color = backgroundColour }
				.Preset(LayoutPreset.FullRect);

			background.UniformPadding(10);

			return new MarginContainer { Name = node.Name + " Container" }
				.Preset(LayoutPreset.FullRect)
				.Add(background, node)
				.SetMarginAll(marginValue);
		}
	}
	[AddOnNodeReady]
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
			this.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Fill)
				.AddAllFontThemeOverride(Colors.Transparent)
				.OverrideStyle(modify: (StyleBoxFlat style) =>
				{
					style.CornerDetail = 1;
					style.BgColor = Colors.Transparent;
					style.SetContentMarginAll(10);
					return style;
				});
			Label.AddThemeFontSizeOverride("normal", 40);

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
	[AddOnNodeReady]
	public sealed partial class MainButtons : HBoxContainer
	{

		public ButtonContainer Container { get; } = new ButtonContainer { Name = "Container", Alignment = AlignmentMode.End }
			.SizeFlags(both: SizeFlags.ExpandFill);
		public Container Spacer { get; } = new BoxContainer { Name = "Spacer", SizeFlagsStretchRatio = 2f }
			.SizeFlags(both: SizeFlags.ExpandFill);
		[AddOnNodeReady]
		public sealed partial class ButtonContainer : VBoxContainer
		{
			public BaseButton Play { get; } = new MainButton(nameof(Play))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton PlayMinesweeper { get; } = new MainButton(nameof(PlayMinesweeper))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton Studio { get; } = new MainButton(nameof(Studio))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton Levels { get; } = new MainButton(nameof(Levels))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton Dialogues { get; } = new MainButton(nameof(Dialogues))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton Settings { get; } = new MainButton(nameof(Settings))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public BaseButton Quit { get; } = new MainButton(nameof(Quit))
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Expand);
			public override void _Ready() => this.SizeFlags(SizeFlags.ExpandFill, SizeFlags.Fill);
		}
	}
	public interface IPress
	{
		void PlayPressed();
		void PlayMinesweeperPressed();
		void OpenStudioPressed();
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
}