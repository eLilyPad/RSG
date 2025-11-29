using Godot;
using static Godot.DisplayServer;

namespace RSG.UI;


public static class VideoSettings
{
	public static OptionButton AttachToServer(this OptionButton options)
	{
		foreach (WindowMode mode in Enum.GetValues<WindowMode>())
		{
			options.AddItem(mode.ToString(), (int)mode);
		}
		options.SetItemDisabled((int)WindowMode.Minimized, true);
		options.Ready += Ready;
		return options;
		void Ready()
		{
			options.ItemSelected += ItemSelected;
			SceneTree tree = options.GetTree();
			tree.ProcessFrame += PreProcess;
			options.TreeExited += () =>
			{
				options.ItemSelected -= ItemSelected;
				tree.ProcessFrame -= PreProcess;
			};
		}
		void PreProcess()
		{
			if (!options.Visible) return;
			int mode = (int)WindowGetMode();
			bool selectedMatches = options.Selected is not -1 && mode != options.Selected;
			if (!selectedMatches) options.Selected = mode;
		}
		static void ItemSelected(long id) => WindowSetMode(id.AsMode());
	}
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
		public OptionButton WindowButtons { get; } = new() { Name = "Window Mode Options" };
		public RichTextLabel WindowsLabel { get; } = new() { Text = "Window Mode : " };
		public HBoxContainer Margin { get; } = new();
		public override void _Ready()
		{
			Name = "VideoContainer";
			this.Add(Margin.Add(WindowsLabel, WindowButtons));

			foreach (WindowMode mode in Enum.GetValues<WindowMode>())
			{
				WindowButtons.AddItem(mode.ToString(), (int)mode);
			}
			WindowButtons.SetItemDisabled((int)WindowMode.Minimized, true);
			WindowButtons.ItemSelected += ItemSelected;

		}
		public override void _Process(double delta)
		{
			if (!WindowButtons.Visible) return;
			int mode = (int)WindowGetMode();
			bool selectedMatches = WindowButtons.Selected is not -1 && mode != WindowButtons.Selected;
			if (!selectedMatches) WindowButtons.Selected = mode;
		}

		private void ItemSelected(long id) => WindowSetMode(id.AsMode());
	}
}
