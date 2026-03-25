using Godot;

namespace RSG;

public abstract class NodePool<TValue> : NodePool<string, TValue> where TValue : Node
{
	private const string defaultName = "";
	public sealed override TValue GetOrCreate(string key)
	{
		TValue value = base.GetOrCreate(key);
		value.Renamed += () => ChangeName(value);
		value.TreeEntered += () => ChangeName(value);
		return value;
	}
	private void ChangeName(TValue renamed)
	{
		var previous = _nodes.FirstOrDefault(
			predicate: pair => pair.Value.GetInstanceId() == renamed.GetInstanceId(),
			defaultValue: new(defaultName, default!)
		);
		if (previous is { Key: defaultName }) return;
		if (previous.Key == renamed.Name) return;
		_nodes[renamed.Name] = renamed;
		_nodes.Remove(previous.Key);
	}
}
public abstract class NodePool<TKey, TValue> where TKey : notnull where TValue : Node
{
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public virtual TValue GetOrCreate(TKey key) => _nodes.GetOrCreate(key, create: Create);
	public void Clear(params IEnumerable<TKey> exceptions)
	{
		foreach ((TKey key, TValue node) in _nodes)
		{
			if (exceptions.Contains(key)) continue;
			Parent(key).Remove(free: true, node);
			_nodes.Remove(key);
		}
	}

	protected abstract TValue Create(TKey key);
	protected abstract Node Parent(TKey key);
}
