using Godot;

namespace RSG.Nonogram;

public abstract class NodePool<TKey, TValue> where TKey : notnull where TValue : Node
{
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public TValue GetOrCreate(TKey key) => _nodes.GetOrCreate(key, create: Create);
	public abstract void Clear(IEnumerable<TKey> exceptions);

	protected abstract TValue Create(TKey key);
	protected void Clear(Func<TKey, Node> parent, IEnumerable<TKey> exceptions)
	{
		foreach ((TKey key, TValue node) in _nodes)
		{
			if (exceptions.Contains(key)) continue;
			parent(key).Remove(free: true, node);
			_nodes.Remove(key);
		}
	}
}
