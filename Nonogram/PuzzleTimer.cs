namespace RSG.Nonogram;

public static class PuzzleTimerExtensions
{
	public static SaveData DisplayTimer<T>(this SaveData save, T config)
	where T : IPuzzleTimer.IHave
	{
		config.Timer.Elapsed = save.TimeTaken;
		return save;
	}
}

public interface IPuzzleTimer
{
	public interface IHave { IPuzzleTimer Timer { get; } }
	public interface IProvider
	{
		void TimeChanged(string value) { }
		void TimeChanged(TimeSpan value) => TimeChanged($"{value.TotalHours:00}:{value.Minutes:00}:{value.Seconds:00}");
	}

	TimeSpan Elapsed { get; set; }
	bool Running { get; set; }
	void TryRun(Display.TileMode input) { if (input is Display.TileMode.Filled) TryRun(); }
	void TryRun() => Running = !Running || Running;
	void Tick(double delta)
	{
		if (!Running) return;
		Elapsed += TimeSpan.FromSeconds(delta);
	}
}
