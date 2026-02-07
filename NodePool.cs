using Godot;

namespace RSG;

public static class PoolExtensions
{
	public static void Update<TKey, TValue, TConfig, TPool>(this TPool pool, TConfig config, IEnumerable<TKey> keys)
	where TKey : notnull
	where TValue : Node
	where TPool : NodePool<TKey, TValue>, Nonogram.IRefresh<TValue, TConfig>, Nonogram.Tile.ISize
	{
		foreach ((TKey _, TValue value) in pool)
		{
			pool.Refresh(value, config);
		}
		pool.Clear(exceptions: keys);
	}
	public static void Refresh<TKey, TValue, TConfig, TPool>(this TPool pool, TConfig config)
	where TKey : notnull
	where TValue : Control
	where TPool : NodePool<TKey, TValue>, Nonogram.IRefresh<TValue, TConfig>, Nonogram.Tile.ISize
	{
		foreach ((TKey _, TValue value) in pool)
		{
			pool.Refresh(value, config);
			value.CustomMinimumSize = pool.TileSize;
		}
	}

}

public abstract class NodePool<TKey, TValue, TConfig> : NodePool<TKey, TValue> where TKey : notnull where TValue : Node
{
	public TValue GetOrCreate(TKey key, TConfig config)
	{
		if (!_nodes.TryGetValue(key, out TValue? value))
		{
			return _nodes[key] = Create(key, config);
		}
		return value;
	}
	protected abstract TValue Create(TKey key, TConfig config);
	protected abstract Node Parent(TKey key, TConfig config);
}
public abstract class NodePool<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
where TKey : notnull
where TValue : Node
{
	public IEnumerable<TKey> Keys => [.. _nodes.Keys];
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public void ReplaceAll(IEnumerable<TKey> keys)
	{
		foreach (TKey key in keys)
		{
			_nodes[key] = Create(key);
		}
		Clear(exceptions: keys);
	}
	public TValue GetOrCreate(TKey key)
	{
		if (!_nodes.TryGetValue(key, out TValue? value))
		{
			return _nodes[key] = Create(key);
		}
		return value;
	}
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

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _nodes.GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();
}
