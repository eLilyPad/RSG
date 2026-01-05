using Godot;
using static Godot.Control;

namespace RSG.Extensions;

public sealed partial class Popup<T>() : Popup where T : Control, new()
{
	public T Control { get; init; } = new();
	public override void _Ready() => this.Add(Control);
}
public sealed partial class Labelled<T> : BoxContainer where T : Control
{
	public required RichTextLabel Label { get; init; }
	public required T Value { get; init; }
	public override void _Ready() => this.Add(Label, Value);
}
public sealed partial class Backgrounded<T> : Container where T : Control
{
	public required ColorRect Background { get; init; }
	public required T Value { get; init; }
	public override void _Ready() => this.Add(Background, Value);

}
public static class UIX
{
	public static T SizeFlags<T>(this T control, SizeFlags horizontal, SizeFlags vertical) where T : Control
	{
		(control.SizeFlagsHorizontal, control.SizeFlagsVertical) = (horizontal, vertical);
		return control;
	}
	public static T UniformPadding<T>(this T node, int margin) where T : Control
	{
		node.OffsetTop = node.OffsetBottom = node.OffsetLeft = node.OffsetRight = margin;
		return node;
	}
	public static T Preset<T>(
		this T control,
		LayoutPreset preset,
		LayoutPresetMode resizeMode = LayoutPresetMode.Minsize,
		int margin = 0
	) where T : Control
	{
		if (control.IsNodeReady()) Set();
		else control.Ready += Set;
		return control;

		void Set() => control.SetAnchorsAndOffsetsPreset(preset, resizeMode, margin);
	}
	public static OneOf<string, NotFound> GetSelectedItem(this OptionButton options)
	{
		if (options.GetItemCount() == 0 && options.Selected == -1) return new NotFound();
		return options.GetItemText(options.Selected);
	}
	public static void ClearItems(this PopupMenu menu)
	{
		for (int i = menu.GetItemCount() - 1; i >= 0; i--)
		{
			menu.RemoveItem(i);
		}
	}

	public static PopupMenu SetItems(
		this PopupMenu menu,
		bool clear,
		params IEnumerable<(string label, OneOf<Key, Texture2D, (Texture2D icon, Key binding)> args, Action pressed)> values
	)
	{
		int id = 0;
		if (clear) menu.ClearItems();
		foreach (var (label, args, pressed) in values)
		{
			switch (args.Value)
			{
				case Key key:
					menu.AddItem(label, id, accel: key);
					break;
				case Texture2D texture:
					menu.AddIconItem(texture, label, id);
					break;
				case (Texture2D texture, Key key):
					menu.AddIconItem(texture, label, id, accel: key);
					break;
				default:
					menu.AddItem(label, id);
					break;
			}
		}
		menu.IdPressed += id =>
		{
			int index = (int)id;
			if (!(values.ElementAtOrDefault(index) is var (label, args, pressed))) { return; }
			pressed();
		};

		++id;
		return menu;

	}
}