using Godot;

namespace RSG.Nonogram;

public static class PuzzleSelectorExtensions
{
	public static T ChangePuzzle<T>(this T display, SaveData save) where T : PuzzleSelector.Display
	{
		display.Background.Color = save.CompletionColour;
		display.Button.Name = (display.Button.Text = display.Name = save.Name) + " Button";
		display.Button.Icon = save.Expected.AsIcon(Core.Colours, 16);
		return display;
	}
	public static T ChangeInput<T>(this T display, SaveData puzzle, UI.MainMenu menu, PuzzleSelector.Display.IPressed handler)
	where T : PuzzleSelector.Display
	{
		display.InputHandler = InputHandler;
		return display;

		void InputHandler(InputEvent input)
		{
			if (input is InputEventMouseButton { Pressed: false }) return;
			if (!MouseButton.Left.IsPressed() && !MouseButton.Right.IsPressed()) return;
			handler.Pressed(display, menu, puzzle);
		}
	}

}

public sealed partial class PuzzleSelector : PanelContainer
{
	public static PackDisplay CreateGamePack() => new PackDisplay.Game { }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
	public static PackDisplay CreateStudioPack() => new PackDisplay.Studio { }
		.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);

	private static Labelled<Container> CreatePuzzles<T>(string name, T container)
	where T : Container => new Labelled<Container>
	{
		Name = "Puzzles Display",
		Label = new RichTextLabel { Name = "Label", Text = name, FitContent = true }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
		Value = container,
		Vertical = true
	}.Preset(LayoutPreset.FullRect);

	public ColorRect Background { get; } = new ColorRect { Name = "Background", Color = Colors.DarkCyan }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public ScrollContainer Scroll { get; } = new ScrollContainer { Name = "Scroll" }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
	public Labelled<Container> Puzzles { get; } = new Labelled<Container>
	{
		Name = "Puzzles",
		Vertical = true,
		Label = new RichTextLabel { Name = "PuzzlesTitle", FitContent = true, Text = "Puzzles" }
			.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepSize),
		Value = new VBoxContainer { Name = "Container" }
			.SizeFlags(both: SizeFlags.ExpandFill)
	}.SizeFlags(both: SizeFlags.ExpandFill);

	public override void _Ready() => this.Add(Background, Scroll.Add(Puzzles));

	public sealed partial class Studio : PanelContainer
	{
		public ScrollContainer Scroll { get; } = new ScrollContainer()
			.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);
		public VBoxContainer Puzzles { get; } = new VBoxContainer()
			.SizeFlags(both: SizeFlags.ExpandFill);
		public override void _Ready() => this.Add(Scroll.Add(Puzzles));
	}

	public abstract partial class PackDisplay : PanelContainer
	{
		public sealed partial class Game : PackDisplay
		{
			public override Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
				"Puzzles",
				container: new GridContainer { Name = "Grid", Columns = 5 }
					.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
			);
		}
		public sealed partial class Studio : PackDisplay;
		public new string Name
		{
			get => base.Name;
			set => base.Name = (Puzzles.Label.Text = value) + " Pack";
		}
		public virtual Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
			"Puzzles",
			container: new VBoxContainer { Name = "List" }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
		);
		public override void _Ready() => this.Add(Puzzles);
	}

	public partial class Display : PanelContainer
	{
		public interface IConfigure<T> where T : Display
		{
			IEnumerable<T> Configure(PuzzleData puzzle, params IEnumerable<T> displays)
			{
				foreach (T display in displays) Configure(display, puzzle);
				return displays;
			}
			T Configure(T display, PuzzleData puzzle);
		}
		public interface IPressed { T Pressed<T>(T display, UI.MainMenu menu, SaveData data) where T : Display; }
		public sealed partial class Game : Display;
		public sealed partial class Studio : Display;
		public ColorRect Background { get; } = new ColorRect { Name = "Background", }
		.Preset(LayoutPreset.LeftWide)
		.SizeFlags(both: SizeFlags.ExpandFill)
		.OverrideStyle((StyleBoxFlat style) =>
		{
			style.SetCornerRadiusAll(0);
			return style;
		});
		public Button Button { get; } = new Button
		{
			VerticalIconAlignment = VerticalAlignment.Top,
			IconAlignment = HorizontalAlignment.Center,
		}
		.OverrideStyle<StyleBoxFlat, Button>(Modify)
		.OverrideStyle<StyleBoxFlat, Button>(Modify, "hover");

		public GuiInputEventHandler InputHandler
		{
			set
			{
				if (field is not null) Button.GuiInput -= field;
				field = value;
				Button.GuiInput += field;
			}
		}
		public override void _Ready() => this
			.Add(Background, Button)
			.SizeFlags(both: SizeFlags.ExpandFill);
		private static StyleBoxFlat Modify(StyleBoxFlat style)
		{
			style.SetCornerRadiusAll(0);
			style.SetContentMarginAll(40);
			return style;
		}

	}
}

