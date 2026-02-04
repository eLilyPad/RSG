namespace RSG.Nonogram;

public interface IPuzzleTimer
{
	TimeSpan Elapsed { get; set; }
	bool Running { get; set; }
	void TryRun() => Running = !Running || Running;
	void Tick(double delta)
	{
		if (!Running) return;
		Elapsed += TimeSpan.FromSeconds(delta);
	}
}

public sealed class PuzzleTimer : IPuzzleTimer
{
	public interface IProvider : IHavePuzzleSettings
	{
		void TimeChanged(string value) { }
	}
	public required IProvider Provider { get; init; }
	public TimeSpan Elapsed { get; set => ChangeTime(field = value); }
	public bool Running { get; set; } = false;
	private void ChangeTime(TimeSpan time)
	{
		Provider.TimeChanged($"{time.TotalHours:00}:{time.Minutes:00}:{time.Seconds:00}");
	}
}
