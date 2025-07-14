namespace RSG.Extensions;

public static class SystemExtensions
{
	public static T Condense<T>(this T hints, int ignoreValue = 0) where T : IList<int>
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
}
