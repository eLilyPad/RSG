using Godot;

namespace RSG.UI;

public sealed partial class Input : Resource
{
	private class KeyBinds
	{
		public readonly Dictionary<Key, Action> Actions = [];
		public readonly Dictionary<Key, string> Names = [];
	}
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

			public void RefreshBindings()
			{
				foreach (Node child in BindingsContainer.GetChildren())
				{
					BindingsContainer.RemoveChild(child);
					child.QueueFree();
				}
				foreach ((Key key, string name) in GetBindings())
				{
					RichTextLabel keyLabel = new RichTextLabel() { Text = $"{name} : {key}", FitContent = true }
					.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill);
					BindingsContainer.AddChild(keyLabel);
				}
			}
		}
	}

	public static IEnumerable<(Key key, string name)> GetBindings()
	{
		foreach (Key key in Bindings.Actions.Keys)
		{
			string name = Bindings.Names[key];
			yield return (key, name);
		}
	}

	public static void Unbind(params Span<Key> bindings)
	{
		foreach (Key key in bindings)
		{
			Bindings.Actions.Remove(key);
			Bindings.Names.Remove(key);
		}
	}
	public static void Bind(
		Container.KeyBindsContainer bindsContainer,
		params ReadOnlySpan<(Key key, Action action, string name)> bindings
	)
	{
		foreach (var (key, action, name) in bindings)
		{
			Bindings.Actions[key] = action;
			Bindings.Names[key] = name;
		}
		bindsContainer.RefreshBindings();
	}
	public static void RunEvent(InputEvent input)
	{
		if (input is not InputEventKey keyEvent || !keyEvent.Pressed) return;
		Key key = keyEvent.Keycode;
		if (Bindings.Actions.TryGetValue(key, out Action? binding))
		{
			binding();
		}
	}

	private static KeyBinds Bindings => field ??= new();
}