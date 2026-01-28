using Godot;

namespace RSG.UI;

public sealed partial class MainMenuSettings : TabContainer
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
