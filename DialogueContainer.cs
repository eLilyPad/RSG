using Godot;

namespace RSG;

public sealed partial class DialogueContainer : MarginContainer
{
	public sealed partial class ProfileContainer : MarginContainer
	{
		public TextureRect ProfileTexture { get; } = new TextureRect
		{
			Name = "Profile Image",
			ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
			StretchMode = TextureRect.StretchModeEnum.KeepAspect,
			SizeFlagsStretchRatio = 0.2f
		}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(ProfileTexture);
	}
	public sealed partial class MessageContainer : Container
	{
		public RichTextLabel Title { get; } = new() { Name = "Title", BbcodeEnabled = true, FitContent = true };
		public RichTextLabel Message { get; } = new() { Name = "Message", BbcodeEnabled = true, FitContent = true };
		public VBoxContainer Container { get; } = new VBoxContainer
		{
			Name = "Container",
			Alignment = BoxContainer.AlignmentMode.Begin,
		}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkGray with { A = 0.5f } }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(Background, Container.Add(Title, Message));
	}
	public BoxContainer Spacer { get; } = new BoxContainer { SizeFlagsStretchRatio = 1 }
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
	public VBoxContainer Container { get; } = new VBoxContainer
	{
		Alignment = BoxContainer.AlignmentMode.End,
		GrowHorizontal = GrowDirection.End,
		GrowVertical = GrowDirection.End,
	}.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize, 30);
	public MessageContainer Message { get; } = new MessageContainer
	{
		GrowHorizontal = GrowDirection.End,
		GrowVertical = GrowDirection.Begin,
		SizeFlagsStretchRatio = 0.6f
	}.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public RichTextLabel Instruction { get; } = new()
	{
		Name = "Instruction",
		FitContent = true,
		Text = "Click to continue...",
		HorizontalAlignment = HorizontalAlignment.Center
	};
	public ProfileContainer Profile { get; } = new ProfileContainer { Name = "Profile Image", SizeFlagsStretchRatio = 0.4f }
		.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill);
	public TextureRect Background { get; } = new TextureRect { Name = "Background Image", }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);

	public override void _Ready() => this.Add(
		Background,
		Container.Add(Spacer, Profile, Message, Instruction)
	);
}