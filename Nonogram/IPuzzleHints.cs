using Godot;

namespace RSG.Nonogram;

using static Display;

public interface IPuzzleHints : IRecalculate<Vector2I>, IRecalculate<HintPosition>, IStringifyHints
{
	void GetRemaining(HintPosition position, int index, out int value);
	void TotalHints(HintPosition position, out int value);
	void HintAt(HintPosition position, int index, out int value);
	bool HasHint(HintPosition position);
}
public interface IStringifyHints
{
	string TextLineAt(HintPosition position);
}
public interface IRecalculate<T> { void Recalculate(T position); }