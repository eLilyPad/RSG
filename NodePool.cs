using Godot;


namespace RSG;

public interface IGetParent<TKey, TConfig> where TKey : notnull
{
	Node Parent(TKey key, TConfig value);
}

public abstract class NodePool<TKey, TValue, TConfig> :
	IEnumerable<KeyValuePair<TKey, TValue>>,
	IGetParent<TKey, TConfig>,
	Nonogram.IRefresh<TKey, TConfig>
	where TKey : notnull
	where TValue : Node
{
	public IEnumerable<TKey> Keys => [.. _nodes.Keys];
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public void Refresh(TConfig config)
	{
		foreach ((TKey key, TValue _) in _nodes)
		{
			Refresh(key, config);
		}
	}
	public IEnumerable<KeyValuePair<TKey, TValue>> ReplaceAll(TConfig config, params IEnumerable<TKey> keys)
	{
		foreach (TKey key in keys)
		{
			TValue value = GetOrCreate(key, config);
			Parent(key, config).ReAdd(value);
			Refresh(key, config);
		}
		Remove(config, _nodes.Keys.Exclude(exceptions: keys));
		return _nodes;
	}
	public TValue GetOrCreate(TKey key, TConfig config)
	{
		if (_nodes.TryGetValue(key, out TValue? value)) return value;
		return _nodes[key] = Create(key, config);
	}
	public virtual void Refresh(TKey key, TValue value, TConfig config) { }
	public virtual void Refresh(TKey key, TConfig config) => Refresh(key, GetOrCreate(key, config), config);
	public abstract Node Parent(TKey key, TConfig config);
	public void Clear(TConfig config, params IEnumerable<TKey> exceptions)
	{
		IEnumerable<TKey> keys = _nodes.Keys.Where(key => !exceptions.Contains(key));
		Remove(config, keys);
	}
	public void Remove(TConfig config, params IEnumerable<TKey> keys)
	{
		foreach (TKey key in keys)
		{
			Assert(_nodes.ContainsKey(key));
			TValue value = _nodes[key];
			Node parent = Parent(key, config);
			parent.Remove(true, value);
			_nodes.Remove(key);
		}
	}
	protected abstract TValue Create(TKey key, TConfig config);

	IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => _nodes.GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => _nodes.GetEnumerator();
}
public abstract class NodePool<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
where TKey : notnull
where TValue : Node
{
	public IEnumerable<TKey> Keys => [.. _nodes.Keys];
	protected readonly Dictionary<TKey, TValue> _nodes = [];
	public TValue GetOrCreate(TKey key)
	{
		if (_nodes.TryGetValue(key, out TValue? value)) return value;
		return _nodes[key] = Create(key);
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
