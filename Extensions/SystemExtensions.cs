namespace RSG.Extensions;

public static class SystemExtensions
{
	public static bool AllEqual<T>(this T expected, params ReadOnlySpan<T> values) where T : notnull
	{
		foreach (T value in values)
		{
			if (!value.Equals(expected)) return false;
		}
		return true;
	}
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
	public static TValue GetOrCreate<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> create)
	where TKey : notnull
	{
		if (!dictionary.TryGetValue(key, out TValue? value))
		{
			return dictionary[key] = create(key);
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
	public static string AddSpacesToPascalCase(this string input)
	{
		if (string.IsNullOrEmpty(input)) return input;
		System.Text.StringBuilder builder = new(input.Length + 5);
		builder.Append(input[0]);
		for (int i = 1; i < input.Length; i++)
		{
			char current = input[i], previous = input[i - 1];
			bool shouldAddSpace = char.IsUpper(current) && (char.IsLower(previous) || char.IsDigit(previous));
			if (shouldAddSpace) builder.Append(' ');
			builder.Append(current);
		}
		return builder.ToString();
	}
}
