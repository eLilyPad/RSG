using Godot;

namespace RSG.UI;

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