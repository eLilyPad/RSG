using Godot;

namespace RSG.UI;

public sealed partial class Video : Resource
{
	public sealed partial class Container : VBoxContainer
	{
		public OptionButton WindowButtons { get; } = new() { Name = "Full Screen Toggle" };
		public RichTextLabel WindowsLabel { get; } = new() { Text = "Window Mode : " };
		public HBoxContainer Margin { get; } = new();
		public override void _Ready()
		{
			Name = "VideoContainer";
			this.Add(Margin.Add(WindowsLabel, WindowButtons));
		}
	}
}
public sealed partial class Input : Resource
{
	public sealed partial class Container : ScrollContainer
	{
		public Button ResetButton { get; } = new() { Text = "Reset" };
		public VBoxContainer InputsContainer { get; } = new();
		public HBoxContainer Margin { get; } = new HBoxContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, 10);
		public override void _Ready()
		{
			Name = "InputContainer";
			this.Add(Margin.Add(ResetButton, InputsContainer));
		}
	}
}

public sealed partial class MainMenu : Container
{
	public required ColourPack Colours { get; init => Background.Color = (field = value).MainMenuBackground; }

	public ColorRect Background { get; } = new ColorRect { Name = nameof(Background) }
	.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
	.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	public SettingsContainer Settings { get; } = new();
	public MainButtons Buttons
	{
		get
		{
			if (field is not null) return field;
			MainButtons buttons = new();
			buttons.Play.Pressed += () => Visible = !Visible;
			buttons.Settings.Pressed += Settings.Show;
			buttons.Quit.Pressed += () => GetTree().Quit();
			return buttons;
		}
	}
	public override void _Ready()
	{
		Name = nameof(MainMenu);
		this.Add(Background, Buttons, Settings)
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
			.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
	}
	public void Step()
	{
		if (!Visible)
		{
			Show();
			return;
		}
		if (Settings.Visible)
		{
			Settings.Hide();
			return;
		}

		Hide();
	}
	public sealed partial class SettingsContainer : TabContainer
	{
		public Audio.Container Audio { get; } = new();
		public Video.Container Video { get; } = new() { Name = "Video" };
		public Input.Container Input { get; } = new() { Name = "Input" };

		public override void _Ready()
		{
			Visible = false;
			this.Add(Audio, Video, Input)
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		}
	}
	public sealed partial class MainButtons : VBoxContainer
	{
		public Button Play { get; } = new() { Name = nameof(Play), Text = nameof(Play) };
		public Button Settings { get; } = new() { Name = nameof(Settings), Text = nameof(Settings) };
		public Button Quit { get; } = new() { Name = nameof(Quit), Text = nameof(Quit) };
		public MainButtons()
		{
			Name = nameof(MainButtons);
			this.Add(Play, Settings, Quit)
				.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize)
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		}
	}
}