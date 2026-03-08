using Godot;

namespace RSG.Nonogram;

using static Display;

sealed class Hints(Hints.IProvider Provider) : NodePool<HintPosition, Hint>
{
	internal interface IProvider : IStringifyHints
	{
		Node Parent(HintPosition position);
	}

	public Vector2 TileSize { get; set; } = Vector2.Zero;
	public IColours Colours { private get; set; } = Core.Colours;

	public void Update(int size)
	{
		IEnumerable<HintPosition> hintValues = HintPosition.AsRange(size);
		foreach (HintPosition position in hintValues)
		{
			Hint hint = GetOrCreate(position);
			ApplyText(position, hint);
		}
		Clear(exceptions: hintValues);
	}
	public void Refresh()
	{
		foreach ((HintPosition position, Hint hint) in _nodes)
		{
			ApplyText(position, hint);
		}
	}
	public void ApplyText(HintPosition position, Hint hint) => hint.Label.Text = Provider.TextLineAt(position)
		?? EmptyHint;
	protected override Node Parent(HintPosition position) => Provider.Parent(position);
	protected override Hint Create(HintPosition position)
	{
		Hint hint = Hint.Create(position, Colours);
		Provider.Parent(position).AddChild(hint);
		hint.CustomMinimumSize = TileSize;
		return hint;
	}
}

public sealed partial class Hint : PanelContainer
{
	public static Hint Create(HintPosition position, IColours colours)
	{
		Hint hint = new Hint
		{
			Name = $"Hint (Side: {position.Side}, Index: {position.Index})",
			Label = new RichTextLabel { Name = "Label", Text = EmptyHint, FitContent = true }
				.SizeFlags(SizeFlags.ExpandFill)
		}.SizeFlags(SizeFlags.ExpandFill);
		(hint.Label.HorizontalAlignment, hint.Label.VerticalAlignment) = position.Alignment();
		hint.Label.AddThemeFontSizeOverride("normal_font_size", 15);
		hint.Background.Color = position.Index % 2 == 0 ? colours.NonogramHintBackground1 : colours.NonogramHintBackground2;
		return hint;
	}
	public required RichTextLabel Label { get; init; }
	public ColorRect Background { get; } = new ColorRect { Name = "Background" }
		.Preset(LayoutPreset.FullRect);
	private Hint() { }
	public override void _Ready() => this.Add(Background, Label);
}
