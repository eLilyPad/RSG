using Godot;

namespace RSG.Nonogram;

public sealed partial class Menu : MenuBar
{
	public Popup<CodeLoaderContainer> CodeLoader { get; } = new()
	{
		Name = "Code Loader",
		Control = new CodeLoaderContainer { Name = "Code Loader Container" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill),
	};
	public Popup<VBoxContainer> PuzzleLoader { get; } = new()
	{
		Name = "Puzzle Loader",
		Control = new VBoxContainer { Name = "Puzzle Loader Container" }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize)
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill),
	};
	public PopupMenu Loader { get; } = new PopupMenu { Name = "Load" };
	public PopupMenu Saver { get; } = new PopupMenu { Name = "Save" };

	public List<PuzzleSelector.PackDisplay> Packs { private get; init; } = [];

	public override void _Ready() => this.Add(Loader.Add(PuzzleLoader, CodeLoader), Saver);

	public partial class CodeLoaderContainer : VBoxContainer
	{
		public const string InputPlaceholder = "Enter Nonogram Code Here ...";
		public LineEdit Input { get; } = new LineEdit { Name = "CodeInput", PlaceholderText = InputPlaceholder }
		.Preset(LayoutPreset.TopLeft, LayoutPresetMode.KeepSize);
		public RichTextLabel Validation { get; } = new RichTextLabel { Name = "Validation", FitContent = true, Text = "Empty" }
		.Preset(LayoutPreset.BottomLeft, LayoutPresetMode.KeepSize);
		public override void _Ready() => this.Add(Input, Validation);
	}
}
