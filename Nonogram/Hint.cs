using Godot;

namespace RSG.Nonogram;

using static Display;

sealed class Hints(Hints.IProvider Provider, IColours Colours) : NodePool<HintPosition, Hint>
{
	internal interface IProvider
	{
		Node Parent(HintPosition position);
		string Text(HintPosition position);
	}
	public Vector2 TileSize { get; set; } = Vector2.Zero;

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
	public void ApplyText(HintPosition position, Hint hint) => hint.Label.Text = Provider.Text(position);
	public override void Clear(IEnumerable<HintPosition> exceptions) => Clear(parent: Provider.Parent, exceptions);
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
				.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill)
		}.SizeFlags(SizeFlags.ExpandFill, SizeFlags.ExpandFill);
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
