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
