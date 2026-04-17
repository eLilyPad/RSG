namespace RSG.Extensions;


public static class ArrayExtensions
{
	public static int[] Fill(this int[] array, int value)
	{
		Array.Fill(array, value);
		return array;
	}
}

public static class SystemExtensions
{
	public static int Remaining<T>(this T hints, int index) where T : IReadOnlyList<int>
	{
		int remaining = 0;
		for (int nextIndex = index + 1; nextIndex < hints.Count; nextIndex++)
		{
			remaining += hints[nextIndex] + 1;
		}
		return remaining;
	}
	public static bool TryGetValue<T, TValue>(this T values, int index, [MaybeNullWhen(false)] out TValue? value)
	where T : IEnumerable<TValue>
	{
		value = default;
		if (index >= values.Count()) return false;
		value = values.ElementAt(index);
		return false;
	}
	public static bool IsSquare<T, TValue>(this T values)
	where T : IEnumerable<KeyValuePair<Godot.Vector2I, TValue>>
	{
		int count = values.Count();
		int size = (int)Math.Sqrt(count);
		return size * size == count;
	}
	public static bool AllEqual<T>(this T expected, params ReadOnlySpan<T> values)
	{
		EqualityComparer<T> comparer = EqualityComparer<T>.Default;
		foreach (T value in values)
		{
			if (!comparer.Equals(value, expected)) return false;
		}
		return true;
	}
	public static T Condense<T>(this T values, out int[] result) where T : IEnumerable<int>
	{
		int size = values.Count(), connected = 0, actualSize = 0;
		result = new int[size];

		foreach ((int i, int value) in values.Index())
		{
			if (value > 0) connected++;
			else
			{
				result[i] = connected;
				connected = 0;
				actualSize++;
			}
		}
		if (connected > 0) result[^1] = connected;
		Array.Resize(ref result, actualSize);
		return values;
	}
	public static TValue GetOrCreate<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TValue> create)
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
