using Godot;

namespace RSG.UI;

public sealed partial class MainMenu : Container
{
	public required ColourPack Colours { get; init => Background.Color = (field = value).MainMenuBackground; }
	public ColorRect Background { get; } = new ColorRect { Name = nameof(Background) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
		.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	public SettingsContainer Settings { get; } = new SettingsContainer { Name = "Settings", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, 100);
	public MainButtons Buttons { get; } = new MainButtons { Name = nameof(Buttons) }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
	public MainMenu() => this.Add(Background, Buttons, Settings);
	public sealed partial class SettingsContainer : TabContainer
	{
		public const int Margin = 0;
		public Audio.Container Audio { get; } = new Audio.Container { Name = "Audio" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public Video.Container Video { get; } = new Video.Container { Name = "Video" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public Input.Container Input { get; } = new Input.Container { Name = "Input" }
			.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepWidth, Margin);
		public SettingsContainer() => this.Add(Audio, Video, Input);
	}
	public sealed partial class MainButtons : VBoxContainer
	{
		public Button Play { get; } = new() { Name = nameof(Play), Text = nameof(Play) };
		public Button Levels { get; } = new() { Name = nameof(Levels), Text = nameof(Levels) };
		public Button Settings { get; } = new() { Name = nameof(Settings), Text = nameof(Settings) };
		public Button Quit { get; } = new() { Name = nameof(Quit), Text = nameof(Quit) };
		public MainButtons() => this.Add(Play, Levels, Settings, Quit);
	}
}