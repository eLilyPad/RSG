using Godot;

namespace RSG;

public abstract class NodePool<TKey, TValue>
where TKey : notnull
where TValue : Node
{
	protected readonly IDictionary<TKey, TValue> _nodes = new Dictionary<TKey, TValue>();
	public TValue GetOrCreate(TKey key) => _nodes.GetOrCreate(key, create: Create);
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

	public abstract class SingleParent(Node parent) : NodePool<TKey, TValue>
	{
		protected override Node Parent(TKey key) => parent;
	}
	public abstract class PooledGrand<TChild>(Node parent) : SingleParent(parent)
	{
		protected readonly IDictionary<TKey, IList<TChild>> _puzzleDisplays = new Dictionary<TKey, IList<TChild>>();
		public IList<TChild> GetGrandChildren(TKey id) => _puzzleDisplays.GetOrCreate(key: id, create: _ => []);
	}
}
