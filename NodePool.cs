using Godot;

namespace RSG;

public abstract class NodePool<TKey, TValue> where TKey : notnull where TValue : Node
{
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public TValue GetOrCreate(TKey key) => _nodes.GetOrCreate(key, create: Create);
	public void Clear(IEnumerable<TKey> exceptions)
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
