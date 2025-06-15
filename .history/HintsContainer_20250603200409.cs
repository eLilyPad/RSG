using Godot;

namespace RSG.UI;

public sealed partial class HintsContainer : BoxContainer
{
	public static (HintsContainer Rows, HintsContainer Columns) Hints(int length)
	{
		return (new HintsContainer(length), new HintsContainer(length));
	}
	private readonly RichTextLabel[][] _hints = [];

	private HintsContainer(int length)
	{
		Name = "Hints";
		SizeFlagsHorizontal = SizeFlags.ExpandFill;
		SizeFlagsVertical = SizeFlags.ExpandFill;
		_hints = new RichTextLabel[length][];

		SetAnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);

		for (int i = 0; i < length; i++)
		{
			_hints[i] = new RichTextLabel[length];
		}
	}

	public override void _Ready()
	{
		for (int i = 0; i < _hints.Length; i++)
		{
			SetHint(line: i, 0, "0");
		}
	}
	public void SetHint(int line, int position, string text)
	{
		if (line < 0 || line >= _hints.Length) { return; }
		RichTextLabel[] lines = _hints[line];
		if (position < 0 || position >= lines.Length) { return; }
		if (lines[position] is RichTextLabel label)
		{
			label.Text = text;
			return;
		}
		lines[position] = new RichTextLabel
		{
			Name = $"Hint ({line}, {position})",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		}.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		this.Add(lines[position]);
	}
}
