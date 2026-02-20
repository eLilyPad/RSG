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
			if (field.TotalSeconds == value.TotalSeconds) return;
			field = value;
			_stringBuilder.Clear();
			_stringBuilder.AppendFormat("{0:00}:{1:00}:{2:00}",
				(int)field.TotalHours,
				field.Minutes,
				field.Seconds
			);
			Provider.TimeChanged(_stringBuilder.ToString());
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

	private readonly System.Text.StringBuilder _stringBuilder = new();

	public void Tick(double delta)
	{
		if (!Running) return;
		Elapsed += TimeSpan.FromSeconds(delta);
	}
	public void TryStart(Display.TileMode tile)
	{
		if (Running || tile is not Display.TileMode.Filled)
		{
			return;
		}
		Running = true;
	}
}
