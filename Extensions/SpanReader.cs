namespace RSG.Extensions;

public ref struct SpanReader(ReadOnlySpan<char> Span)
{
	private int _pos = 0;
	private readonly ReadOnlySpan<char> _span = Span;

	public bool TryReadToken(out ReadOnlySpan<char> token)
	{
		while (_pos < _span.Length && char.IsWhiteSpace(_span[_pos]))
			_pos++;

		if (_pos >= _span.Length)
		{
			token = default;
			return false;
		}

		int start = _pos;
		while (_pos < _span.Length && !char.IsWhiteSpace(_span[_pos]))
			_pos++;

		token = _span[start.._pos];
		return true;
	}
}
