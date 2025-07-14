using Godot;

namespace RSG.Extensions;

public static class GDX
{
	public static T LoadOrCreateResource<T>(this string path) where T : Resource, new()
	{
		var resource = GD.Load<T>(path);
		if (resource == null || resource == default)
		{
			resource = new();
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
	public static T Remove<T>(this T parent, params Span<Node> children) where T : Node
	{
		foreach (Node node in children)
		{
			if (!node.IsInsideTree() || !parent.HasNode(node.GetPath())) { continue; }
			parent.RemoveChild(node);
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
	public static bool TryHide(this Node node)
	{
		switch (node)
		{
			case Node2D container when container.IsInsideTree() && container.Visible:
				container.Hide();
				return true;
			case Window window when window.IsInsideTree() && window.Visible:
				window.Hide();
				return true;
			default: return false;
		}
	}


}