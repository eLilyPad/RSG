namespace RSG.Nonogram;

public readonly record struct Settings()
{
	public bool LockCompletedFilledTiles { get; init; } = true;
	public bool LockCompletedBlockedTiles { get; init; } = true;
	public bool LineCompleteBlockRest { get; init; } = true;
	public bool ShowMistakes { get; init; } = true;
	public bool HaveTimer { get; init; } = true;
}
