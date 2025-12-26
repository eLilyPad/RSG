namespace RSG.Extensions;

public static class SystemExtensions
{
	public static T Condense<T>(this T hints, int ignoreValue = 0)
	where T : IList<int>
	{
		for (int i = 0; i < hints.Count; i++)
		{
			int current = hints[i], previous = hints.ElementAtOrDefault(i - 1);
			if (current == ignoreValue) continue;
			if (i > ignoreValue && previous != ignoreValue)
			{
				hints[i - 1] += current;
				hints.RemoveAt(i);
				i--;
			}
		}
		return hints;
	}
	public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TValue> create)
	where TKey : notnull
	{
		if (!dictionary.TryGetValue(key, out TValue? value))
		{
			return dictionary[key] = create();
		}
		return value;
	}
	public static Dictionary<TKey, TValueTo> ToDictionary<TKey, TValueFrom, TValueTo>(
		this Dictionary<TKey, TValueFrom> dict,
		Func<KeyValuePair<TKey, TValueFrom>, TValueTo> elementSelector
	)
	where TKey : notnull
	{
		return dict.ToDictionary(keySelector: pair => pair.Key, elementSelector);
	}
	public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(
		this IEnumerable<TKey> array,
		Func<TKey, TValue> elementSelector
	)
	where TKey : struct
	{
		return array.ToDictionary(keySelector: key => key, elementSelector);
	}
}
