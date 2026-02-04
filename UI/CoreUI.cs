using Godot;

namespace RSG.UI;

using RSG.Console;
using RSG.Dialogue;

public sealed partial class CoreUI : Control
{
	private const int MenuIndex = 0, LoadingIndex = 1;
	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer
	{
		Name = "Loading Screen",
		TopLevel = true,
		ZIndex = LoadingIndex
	}
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu
	{
		get
		{
			if (field is not null) return field;
			field = new MainMenu
			{
				Name = "MainMenu",
				TopLevel = true,
				ZIndex = MenuIndex
			}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
			AddChild(field);
			return field;
		}
	}
	public required ColourPack Colours { set => Menu.Colours = value; }

	public override void _Ready() => this.Add(Dialogues.Container, Console.Container, LoadingScreen);
}

