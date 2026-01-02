namespace RSG.Nonogram;

public sealed class PuzzleTimer
{
	public required Action<string> TimeChanged { get; init; }
	public TimeSpan Elapsed
	{
		get; set
		{
			field = value;
			TimeChanged($"{field.TotalHours:00}:{field.Minutes:00}:{field.Seconds:00}");
		}
	}
	public bool Running { get; set; } = false;
	public void Tick(double delta)
	{
		if (!Running) return;
		Elapsed += TimeSpan.FromSeconds(delta);
	}
}
