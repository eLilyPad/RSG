using Godot;

namespace RSG.Extensions;

public static class VectorExtensions
{
	public static bool EitherEqual(this Vector2I position, Vector2I other) => other.X == position.X || other.Y == position.Y;
	public static int Squared(this Vector2I value) => value.X * value.X + value.Y * value.Y;
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
	public static bool IsInBorder(this Vector2I size, Vector2I position, int thickness = 1)
	{
		if (thickness <= 0) return false;

		// Optional safety check
		if (position.X < 0 || position.Y < 0 || position.X >= size.X || position.Y >= size.Y) return false;

		return position.X < thickness
			|| position.Y < thickness
			|| position.X >= size.X - thickness
			|| position.Y >= size.Y - thickness;
	}
	public static bool IsCorner(this Vector2I size, Vector2I position)
	{
		return (position.X == 0 && position.Y == 0)
			|| (position.X == size.X - 1 && position.Y == 0)
			|| (position.X == 0 && position.Y == size.Y - 1)
			|| (position.X == size.X - 1 && position.Y == size.Y - 1);
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
public static class ImageExtensions
{
	public static void SetPixel(this Image image, int x, int y, Color color, int size)
	{
		int half = size / 2;

		for (int dx = -half; dx <= half; dx++)
		{
			for (int dy = -half; dy <= half; dy++)
			{
				int px = x + dx;
				int py = y + dy;

				if (image.PixelInImage(px, py))
				{
					image.SetPixel(px, py, color);
				}
			}
		}
	}
	public static bool PixelInImage(this Image image, int x, int y)
	{
		return x >= 0 && y >= 0 && x < image.GetWidth() && y < image.GetHeight();
	}
}
public static class GDX
{
	public static void LinkToParent<T>(this Node node, List<T> list) where T : Node
	{
		node.ChildEnteredTree += OnChildEnteredTree;
		node.ChildExitingTree += OnChildExitingTree;

		void OnChildExitingTree(Node node)
		{
			if (node is T display && list.Contains(display))
			{
				list.Remove(display);
			}
		}
		void OnChildEnteredTree(Node node)
		{
			if (!GodotObject.IsInstanceValid(node)) return;
			if (node is T display)
			{
				list.Add(display);
			}
		}
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


	public static T Add<T>(this T parent, params IEnumerable<Node> children) where T : Node
	{
		foreach (Node node in children) { parent.AddChild(node); }
		return parent;
	}
	public static T RemoveChildren<T>(this T parent, bool free = false) where T : Node
	{
		IEnumerable<Node> children = parent.GetChildren();
		return parent.Remove(free, children);
	}
	public static T Remove<T>(this T parent, bool free = false, params IEnumerable<Node> children) where T : Node
	{
		foreach (Node node in children)
		{
			//if (!parent.HasChild(node)) { continue; }
			if (node.IsAncestorOf(parent)) { continue; }
			if (!GodotObject.IsInstanceValid(parent)) { continue; }
			if (!GodotObject.IsInstanceValid(node)) { continue; }
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
			&& parent.IsInsideTree()
			&& parent.HasNode(child.GetPath());
	}
	/// <summary>
	/// Hides the control if visible, unless there is a control in steps that is visible. 
	/// The visible step instead will be hidden 
	/// </summary>
	/// <param name="node"></param>
	/// <param name="steps">checks if each step is visible, hides it and exits the method</param>
	public static void StepBack(this Control node, params Span<Control> steps)
	{
		if (!node.Visible)
		{
			node.Show();
			return;
		}
		foreach (Control control in steps)
		{
			if (control.Visible)
			{
				control.Hide();
				return;
			}
		}
		node.Hide();
	}
}