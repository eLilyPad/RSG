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
	public static bool AllEqual<T>(this T expected, params ReadOnlySpan<T> values)
	{
		EqualityComparer<T> comparer = EqualityComparer<T>.Default;
		foreach (T value in values)
		{
			if (!comparer.Equals(value, expected)) return false;
		}
		return true;
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
