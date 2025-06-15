using Godot;
using static Godot.Control;

namespace RSG;

public static class GDExtensions
{
	public static T LoadOrCreateResource<T>(this string path) where T : Resource, new()
	{
		var resource = GD.Load<T>(path);
		if (resource == null || resource == default)
		{
			resource = new T();
			ResourceSaver.Save(resource, path);
		}
		return resource;
	}

	public static IEnumerable<Vector2I> AsRange(this Vector2I size, Vector2I? start = null)
	{
		for (int x = start?.X ?? 0; x < size.X; x++)
		{
			for (int y = start?.Y ?? 0; y < size.Y; y++)
			{
				yield return new Vector2I(x, y);
			}
		}
	}
	public static T Add<T>(this T parent, params Span<Node> children) where T : Node
	{
		foreach (Node node in children)
		{
			parent.AddChild(node);
		}
		return parent;
	}
	public static void FreeAll<TKey, TNode>(this Dictionary<TKey, TNode> nodes, Node? parent = null)
	where TKey : notnull
	where TNode : Node
	{
		foreach (var (key, node) in nodes)
		{
			if (parent is not null && parent.HasNode(node.GetPath()))
			{
				parent.RemoveChild(node);
			}
			nodes.Remove(key);
			node.QueueFree();
		}
	}
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
		node.OffsetTop = margin;
		node.OffsetBottom = margin;
		node.OffsetLeft = margin;
		node.OffsetRight = margin;
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