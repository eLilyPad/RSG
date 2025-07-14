using Godot;
using static Godot.Control;

namespace RSG.Extensions;

public static class UIX
{
	public static T SizeFlags<T>(
		this T control,
		SizeFlags horizontal,
		SizeFlags vertical
	) where T : Control
	{
		(control.SizeFlagsHorizontal, control.SizeFlagsVertical) = (horizontal, vertical);
		return control;
	}
	public static T UniformPadding<T>(this T node, int margin) where T : Control
	{
		node.OffsetTop = node.OffsetBottom = node.OffsetLeft = node.OffsetRight = margin;
		return node;
	}
	public static T AnchorsAndOffsetsPreset<T>(
		this T control,
		LayoutPreset preset,
		LayoutPresetMode resizeMode,
		int margin = 0
	) where T : Control
	{
		if (control.IsNodeReady()) Set();
		else control.Ready += Set;
		return control;

		void Set() => control.SetAnchorsAndOffsetsPreset(preset: preset, resizeMode: resizeMode, margin: margin);
	}
}