using Godot;

namespace RSG;


public sealed partial class DialogueContainer : PanelContainer
{

	public const int ProfileSize = 64;
	public BoxContainer Spacer { get; } = new BoxContainer() { SizeFlagsStretchRatio = 1 }
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
	public VBoxContainer Container { get; } = new VBoxContainer
	{
		Alignment = BoxContainer.AlignmentMode.End,
		GrowHorizontal = GrowDirection.End,
		GrowVertical = GrowDirection.End,
	}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 30);
	public VBoxContainer MessageContainer { get; } = new VBoxContainer
	{
		Alignment = BoxContainer.AlignmentMode.Begin,
		GrowHorizontal = GrowDirection.End,
		GrowVertical = GrowDirection.Begin,
		SizeFlagsStretchRatio = 0.6f
	}.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public RichTextLabel Title { get; } = new() { Name = "Title", FitContent = true, Text = "Title" };
	public RichTextLabel Message { get; } = new() { Name = "Message", FitContent = true, Text = "Message" };
	public RichTextLabel Instruction { get; } = new()
	{
		Name = "Instruction",
		FitContent = true,
		Text = "Click to continue...",
		HorizontalAlignment = HorizontalAlignment.Center
	};
	public TextureRect Profile { get; } = new TextureRect
	{
		Name = "Profile Image",
		ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
		StretchMode = TextureRect.StretchModeEnum.KeepAspect,
		SizeFlagsStretchRatio = 0.2f
	}.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.Fill);
	public TextureRect Background { get; } = new TextureRect { Name = "Background Image", }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
	public MarginContainer ProfileContainer { get; } = new MarginContainer { Name = "ProfileContainer", }
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);

	public DialogueResources Resources
	{
		set
		{
			Profile.Texture = value.Profile;
			Background.Texture = value.Background1;
		}
	}

	public override void _Ready()
	{
		this.Add(
			Background,
			Container.Add(
				Spacer,
				ProfileContainer.Add(Profile),
				MessageContainer.Add(Title, Message),
				Instruction
			)
		);
	}

}