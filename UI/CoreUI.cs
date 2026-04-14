using Godot;

namespace RSG.UI;

using Nonogram;
using RSG.Console;
using RSG.Dialogue;

public static class CoreExtensions
{
	public static CoreUI SetMenu<T>(this CoreUI ui, T value)
	where T : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		Assert(value is not null, "Menu handler cannot be null");
		ui.Menu.Signals = value;
		ui.Menu.OnPressed = value;
		return ui;
	}
	public static CoreUI SetSettings<T>(this CoreUI ui, T value)
	where T : SettingsMenuContainer.IChangeSettings
	{
		Assert(value is not null, "Settings modifier cannot be null");
		ui.Menu.Settings.Nonogram.SettingsChanger = value;
		return ui;
	}
}
public sealed partial class CoreUI : Control
{
	public static CoreUI Create(Node parent)
	{
		CoreUI ui = new CoreUI { Name = "Core UI" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
		parent.AddChild(ui);
		return ui;
	}
	public TitleScreenContainer LoadingScreen { get; } = new TitleScreenContainer
	{
		Name = "Loading Screen",
		TopLevel = true
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public MainMenu Menu { get; } = new MainMenu
	{
		Name = "MainMenu",
		Colours = Core.Colours,
		TopLevel = true
	}.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);
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

