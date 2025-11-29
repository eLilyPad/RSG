using Godot;

namespace RSG.UI;

public sealed partial class Input : Resource
{
	public sealed partial class Container : ScrollContainer
	{
		public KeyBindsContainer InputsContainer { get; } = new KeyBindsContainer { Name = "KeyBindsContainer" };
		public VBoxContainer MainContainer { get; } = new VBoxContainer { Name = "Container" }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public override void _Ready()
		{
			this.Add(MainContainer.Add(InputsContainer));
		}
		public sealed partial class KeyBindsContainer : VBoxContainer
		{
			public Button ResetButton { get; } = new() { Name = "Reset Button", Text = "Reset" };
			public HBoxContainer BindingsContainer { get; } = new HBoxContainer { Name = "Bindings Container" }
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

			public override void _Ready()
			{
				this.Add(ResetButton, BindingsContainer)
					.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.Fill);
			}
		}
	}
}