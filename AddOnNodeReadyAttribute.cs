using Godot;

namespace RSG;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AddOnNodeReadyAttribute : Attribute
{
	private const string Name = nameof(AddOnNodeReadyAttribute);
	public static void ConnectToScene()
	{
		if (Engine.GetMainLoop() is not SceneTree tree)
		{
			GD.PrintErr("Main loop is not a SceneTree");
			return;
		}
		tree.NodeAdded += WhenNodeAdded;
	}
	public static bool HasAttribute(Node node) => HasAttribute(type: node.GetType());
	public static bool HasAttribute(Type type) => type
		.GetCustomAttributes(typeof(AddOnNodeReadyAttribute), inherit: true).Length > 0;
	public static void WhenNodeAdded(Node node)
	{
		var nodeType = node.GetType();
		var hasAttribute = HasAttribute(nodeType);
		var nodeName = nodeType.Name;

		if (!hasAttribute) return;
		else GD.Print($"Node {nodeName} has the {Name} attribute");

		var children = node.GetNodeProperties();
		GD.Print($"Node {nodeName} has {children.Count()} children to add.");

		if (node.IsNodeReady())
			AddChildren();
		else
			node.Ready += AddChildren;

		void AddChildren() => node.Add(children);
	}
}