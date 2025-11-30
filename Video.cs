using Godot;
using static Godot.DisplayServer;

namespace RSG.UI;

public static class VideoSettings
{
	public enum AspectRatio { _1by1, _4by3, _16by9, _16by10, _21by9 }
	public enum Resolutions : long { _1280x720 = 0, _1920x1080 = 1 }
	public static WindowMode AsMode(this long mode) => mode switch
	{
		0 => WindowMode.Windowed,
		1 => WindowMode.Minimized,
		2 => WindowMode.Maximized,
		3 => WindowMode.Fullscreen,
		4 => WindowMode.ExclusiveFullscreen,
		_ => WindowMode.Windowed
	};
	public static OptionButton SetUpItems(this OptionButton button)
	{
		foreach (WindowMode mode in Enum.GetValues<WindowMode>())
		{
			button.AddItem(mode.ToString(), (int)mode);
		}
		button.SetItemDisabled((int)WindowMode.Minimized, true);
		button.SetItemDisabled((int)WindowMode.Windowed, true);
		button.ItemSelected += id => WindowSetMode(id.AsMode());
		return button;
	}
	public static CheckBox ChangeBorderless(this CheckBox check)
	{
		check.Pressed += Pressed;
		return check;
		void Pressed()
		{
			bool isChecked = check.IsPressed();
			GD.Print("set borderless to ", isChecked);
			WindowSetFlag(WindowFlags.Borderless, isChecked);
		}
	}
	public static void UpdateToCurrentMode(this OptionButton button)
	{
		if (!button.Visible) return;
		int mode = (int)WindowGetMode();
		bool selectedMatches = button.Selected is not -1 && mode != button.Selected;
		if (!selectedMatches) button.Selected = mode;
	}
}

public sealed partial class Video : Resource
{
	public sealed partial class Container : VBoxContainer
	{
		public Labelled<CheckBox> Borderless { get; } = new Labelled<CheckBox>
		{
			Name = "Borderless Container",
			Value = new CheckBox { Name = "Borderless Options" }.ChangeBorderless(),
			Label = new RichTextLabel { Name = "Label", Text = "Borderless : ", FitContent = true }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		};
		public Labelled<OptionButton> VideoMode { get; } = new Labelled<OptionButton>
		{
			Name = "Video Mode Container",
			Value = new OptionButton { Name = "Video Mode Options" }
			.SetUpItems(),
			Label = new RichTextLabel { Name = "Label", Text = "Video Mode : ", FitContent = true }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		};
		//public Labelled<OptionButton> Resolutions { get; } = new Labelled<OptionButton>
		//{
		//	Name = "Video Resolution Container",
		//	Value = new OptionButton { Name = "Video Resolution" },
		//	Label = new RichTextLabel { Name = "Label", Text = "Resolution : ", FitContent = true }
		//	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		//};
		//public Labelled<OptionButton> VideoAspectRatio { get; } = new Labelled<OptionButton>
		//{
		//	Name = "Video Ratio Container",
		//	Value = new OptionButton { Name = "Video Aspect Ratios" },
		//	Label = new RichTextLabel { Name = "Label", Text = "Video Mode : ", FitContent = true }
		//	.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		//}
		//.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin);
		public override void _Ready() => this.Add(VideoMode, Borderless);
		public override void _Process(double delta)
		{
			VideoMode.Value.UpdateToCurrentMode();
		}
	}
}
