using Godot;

namespace RSG;

public sealed partial class ConsoleContainer : PanelContainer
{
	public sealed partial class LogContainer : BoxContainer
	{
		public RichTextLabel Label { get; } = new RichTextLabel
		{
			Name = "Log Label",
			FitContent = true,
			ScrollFollowing = true
		}.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public ScrollContainer Scroll { get; } = new ScrollContainer
		{
			FollowFocus = true,
			GrowVertical = GrowDirection.End,
			VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
			ScrollHorizontalCustomStep = 0.1f
		}.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public override void _Ready() => this.Add(Scroll.Add(Label));
	}
	public sealed partial class TitleContainer : BoxContainer
	{
		public RichTextLabel Label { get; } = new()
		{
			FitContent = true,
			HorizontalAlignment = HorizontalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.Off,
			Text = "Console"
		};
		public override void _Ready() => this.Add(Label);
	}
	public sealed partial class InputContainer : VBoxContainer
	{
		public const string Placeholder = "Enter Command Here.";
		public LineEdit Line { get; } = new LineEdit
		{
			Name = "Input Line",
			PlaceholderText = Placeholder,
			CaretBlink = true
		}.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill);
		public ItemList SuggestionDisplay = new() { CustomMinimumSize = new(x: 0, y: 100), };
		public override void _Ready() => this.Add(SuggestionDisplay, Line);
	}

	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.Black }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public VBoxContainer Container { get; } = new VBoxContainer
	{
		Name = "Container",
		Alignment = BoxContainer.AlignmentMode.Begin
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public TitleContainer Title { get; } = new TitleContainer { Name = "Title Container" }
		.Preset(preset: LayoutPreset.Center);
	public LogContainer Log { get; } = new LogContainer { Name = "Log Container" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public InputContainer Input { get; } = new InputContainer { Name = "Input Container", }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill);

	public PopupMenu Menu { get; } = new PopupMenu { Name = "Console Menu" };

	public override void _Ready() => this.Add(Menu, Background, Container.Add(Title, Log, Input));
}

