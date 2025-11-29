using Godot;
using static Godot.DisplayServer;

namespace RSG.UI;

public static class VideoSettings
{
	public static WindowMode AsMode(this long mode) => mode switch
	{
		0 => WindowMode.Windowed,
		1 => WindowMode.Minimized,
		2 => WindowMode.Maximized,
		3 => WindowMode.Fullscreen,
		4 => WindowMode.ExclusiveFullscreen,
		_ => WindowMode.Windowed
	};
}

public sealed partial class Video : Resource
{
	public sealed partial class Container : VBoxContainer
	{
		public Labelled<OptionButton> VideoMode { get; } = new Labelled<OptionButton>
		{
			Name = "Video Mode Container",
			Value = new() { Name = "Video Mode Options" },
			Label = new RichTextLabel { Name = "Label", Text = "Video Mode : ", FitContent = true }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		}
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin);
		public HBoxContainer Margin { get; } = new HBoxContainer { Name = "Margin Container" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public override void _Ready()
		{
			this.Add(Margin.Add(VideoMode));

			foreach (WindowMode mode in Enum.GetValues<WindowMode>())
			{
				VideoMode.Value.AddItem(mode.ToString(), (int)mode);
			}
			VideoMode.Value.SetItemDisabled((int)WindowMode.Minimized, true);
			VideoMode.Value.ItemSelected += ItemSelected;

		}
		public override void _Process(double delta)
		{
			if (!VideoMode.Value.Visible) return;
			int mode = (int)WindowGetMode();
			bool selectedMatches = VideoMode.Value.Selected is not -1 && mode != VideoMode.Value.Selected;
			if (!selectedMatches) VideoMode.Value.Selected = mode;
		}

		private void ItemSelected(long id) => WindowSetMode(id.AsMode());
	}
}
