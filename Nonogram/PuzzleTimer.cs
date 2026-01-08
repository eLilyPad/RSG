namespace RSG.Nonogram;

public sealed class PuzzleTimer
{
	public interface IProvider : IHavePuzzleSettings
	{
		void TimeChanged(string value) { }
	}

	public required IProvider Provider { get; init; }

	public TimeSpan Elapsed
	{
		get; set
		{
			field = value;
			Provider.TimeChanged($"{field.TotalHours:00}:{field.Minutes:00}:{field.Seconds:00}");
		}
	}
	public bool Running
	{
		get => Provider.Settings.HaveTimer && field; set
		{
			if (!Provider.Settings.HaveTimer)
			{
				field = false;
				return;
			}
			field = value;
		}
	} = false;
	public void Tick(double delta)
	{
		if (!Running) return;
		Elapsed += TimeSpan.FromSeconds(delta);
	}
}
