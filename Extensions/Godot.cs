using Godot;

namespace RSG.Extensions;

public static class GDX
{
	public static void RefillGrid<TValue>(
		this IDictionary<Vector2I, TValue> nodes,
		int size,
		Func<Vector2I, TValue> create,
		OneOf<Node, Func<Vector2I, Node>> parent,
		OneOf<Action<Vector2I>, Action<TValue>> reset
	)
	where TValue : Node
	{
		nodes.Refill(values: (Vector2I.One * size).GridRange(), create, parent, reset);
	}
	public static void Refill<TKey, TValue>(
		this IDictionary<TKey, TValue> nodes,
		IEnumerable<TKey> values,
		Func<TKey, TValue> create,
		OneOf<Node, Func<TKey, Node>> parent,
		OneOf<Action<TKey>, Action<TValue>> reset
	)
	where TValue : Node
	where TKey : notnull
	{
		foreach (var position in values)
		{
			if (!nodes.TryGetValue(position, out TValue? node))
			{
				node = nodes[position] = create(position);
				Parent(position).AddChild(node);
			}
			reset.Switch(position.PassOn(), node.PassOn());
		}
		foreach (var position in nodes.Keys.Except(values))
		{
			if (!nodes.TryGetValue(position, out var node)) { continue; }
			nodes.Remove(position);
			Parent(position).RemoveChild(node);
			node.QueueFree();
		}
		Node Parent(TKey position) => parent.Match(node => node, getParent => getParent(position));
	}

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

	public static int Squared(this Vector2I value)
	{
		return value.X * value.X + value.Y * value.Y;
	}
	public static IEnumerable<Vector2I> GridRange(this Vector2I size, Vector2I? startAt = null)
	{
		if (startAt is not Vector2I start) { start = Vector2I.Zero; }
		for (int x = start.X; x < size.X; x++)
		{
			for (int y = start.Y; y < size.Y; y++)
			{
				yield return new Vector2I(x, y);
			}
		}
	}
	public static T Add<T>(this T parent, params IEnumerable<Node> children) where T : Node
	{
		foreach (Node node in children) { parent.AddChild(node); }
		return parent;
	}
	public static T Remove<T>(this T parent, bool free = false, params IEnumerable<Node> children) where T : Node
	{
		foreach (Node node in children)
		{
			//if (!parent.HasChild(node)) { continue; }
			if (node.IsAncestorOf(parent)) { continue; }
			parent.RemoveChild(node);
			if (free) node.QueueFree();
		}
		return parent;
	}
	public static T AddOrRemove<T>(this T parent, bool add, bool free = false, params IEnumerable<Node> children) where T : Node
	{
		if (add) parent.Add(children);
		else parent.Remove(free, children);
		return parent;
	}
	public static void FreeAll<TKey, TNode>(this Dictionary<TKey, TNode> nodes, Node? parent = null)
	where TKey : notnull
	where TNode : Node
	{
		foreach (var (key, node) in nodes)
		{
			if (parent is not null && parent.HasChild(node))
			{
				parent.RemoveChild(node);
			}
			nodes.Remove(key);
			node.QueueFree();
		}
	}
	public static T ReplaceChild<T>(this T parent, Node old, Node replacement, bool free = false) where T : Node
	{
		if (parent.HasChild(old))
		{
			parent.RemoveChild(old);
		}
		if (free)
		{
			old.QueueFree();
		}
		parent.Add(replacement);
		return parent;
	}
	public static bool HasChild<T>(this T parent, Node child) where T : Node
	{
		return GodotObject.IsInstanceValid(child)
			&& child.IsInsideTree()
			&& parent.HasNode(child.GetPath());
	}
	public static bool TryParse(this string? value, out Vector2I result)
	{
		result = Vector2I.Zero;
		if (string.IsNullOrEmpty(value)) { return false; }
		foreach ((int index, string part) in value.Trim('(', ')').Split(',').Index())
		{
			if (!int.TryParse(part, out int number))
			{
				GD.PrintErr($"Error parsing int from string part: {part}");
				return false;
			}
			switch (index)
			{
				case 0: result.X = number; break;
				case 1: result.Y = number; break;
			}
		}
		return true;
	}
}