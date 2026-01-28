using Godot;

namespace RSG.UI;

public sealed partial class MainMenuButtons : HBoxContainer
{
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
