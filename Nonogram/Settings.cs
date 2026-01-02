namespace RSG.Nonogram;

public sealed record class Settings
{
	public bool LineCompleteLock { get; set; } = true;
	public bool LineCompleteBlockRest { get; set; } = true;
	public bool HaveTimer { get; set; } = true;
}
