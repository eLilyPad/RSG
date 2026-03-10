using Godot;

namespace RSG.UI;

using Nonogram;
using RSG.Console;
using RSG.Dialogue;

public sealed partial class CoreUI : Control
{
	public static CoreUI Create<TSettings, TMenu>(
		Node parent,
		ColourPack colours,
		TMenu menu,
		TSettings settings
	)
	where TSettings : SettingsMenuContainer.IChangeSettings, PuzzleManager.IChangeWithSettings
	where TMenu : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		CoreUI ui = new CoreUI { Name = "Core UI" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
		parent.AddChild(ui);
		ui.Menu.Colours = colours;
		ui.Menu.Signals = menu;
		ui.Menu.OnPressed = menu;
		ui.Menu.Settings.Nonogram.SettingsChanger = settings;
		return ui;
	}
	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer { Name = "Loading Screen", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu { get; } = new MainMenu { Name = "MainMenu", TopLevel = true }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
	public ReadOnlySpan<Container> Escapable => _escapableContainers;
	private readonly Container[] _escapableContainers;
	public CoreUI() => _escapableContainers = [Menu.Settings, Menu.Levels, Menu.Dialogues];
	public override void _Ready() => this.Add(Dialogues.Container, Console.Container, Menu, LoadingScreen);
	public void ShowMainMenu()
	{
		Menu.Show();
		Menu.Buttons.Show();
	}
	public void ShowMainMenu(out bool shown)
	{
		shown = false;
		if (Menu.Visible) { return; }
		ShowMainMenu();
		shown = true;
	}
}

