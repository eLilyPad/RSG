using Godot;

namespace RSG.Nonogram;

public abstract class Displays<TPack, TDisplay>(IColours colours, UI.MainMenu menu, Node parent)
: NodePool<string, TPack>.PooledGrand<TDisplay>(parent)
where TPack : PuzzleSelector.PackDisplay, new()
where TDisplay : PuzzleSelector.PuzzleDisplay, new()
{
	public void Load(IEnumerable<(string Name, IEnumerable<SaveData> Puzzles)>? configs = null)
	{
		configs ??= PuzzleManager.SelectorConfigs;
		foreach ((string Name, IEnumerable<SaveData> Puzzles) in configs)
		{
			IList<TDisplay> displays = GetGrandChildren(Name);
			var parent = GetOrCreate(Name).Puzzles.Value;
			foreach ((int i, SaveData puzzle) in Puzzles.Index())
			{
				if (displays.Count <= i)
				{
					var display = new TDisplay { Name = puzzle.Name };
					parent.AddChild(display);
					displays.Add(Configure(display, puzzle));
				}
				else Configure(displays[i], puzzle);
			}
		}
	}
	protected abstract void Pressed(TDisplay display, SaveData puzzle, MouseButton button);
	protected virtual Texture2D GetIcon(SaveData puzzle) => puzzle.AsIcon(colours);
	protected virtual TDisplay Configure(TDisplay display, SaveData puzzle)
	{
		//display.Background.Color = colours.CompletionColour(puzzle);
		display.Button.Name = (display.Button.Text = display.Name = puzzle.Name) + " Button";
		display.Button.Icon = GetIcon(puzzle);
		display.InputHandler = InputHandler;

		return display;

		void InputHandler(InputEvent input)
		{
			if (input is not InputEventMouseButton mouseInput) return;
			if (!mouseInput.Pressed) return;

			Assert(
				condition: GodotObject.IsInstanceValid(menu),
				"Menu display must be valid"
			);
			Assert(condition: GodotObject.IsInstanceValid(menu.Levels),
				"Levels display must be valid"
			);

			if (MouseButton.Left.IsPressed()) Pressed(display, puzzle, button: MouseButton.Left);
			if (MouseButton.Right.IsPressed()) Pressed(display, puzzle, button: MouseButton.Right);
		}
	}
	protected override TPack Create(string key)
	{
		var pack = new TPack { Name = key }
			.Preset(Control.LayoutPreset.FullRect, Control.LayoutPresetMode.KeepSize);
		Parent(key).AddChild(pack);
		return pack;
	}
}

public sealed partial class PuzzleSelector : PanelContainer
{
	private static Labelled<Container> CreatePuzzles<T>(string name, T container)
	where T : Container
	{
		return new Labelled<Container>
		{
			Name = "Puzzles Display",
			Label = new RichTextLabel { Name = "Label", Text = name, FitContent = true }
			.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ShrinkBegin),
			Value = container,
			Vertical = true
		}.Preset(LayoutPreset.FullRect);
	}
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
		public static PackDisplay CreateForGame((string Name, IEnumerable<SaveData> Data) config)
		{
			return new GamePacks { Name = config.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		}
		public static PackDisplay CreateForStudio((string Name, IEnumerable<SaveData> Data) config)
		{
			return new StudioPacks { Name = config.Name }
				.Preset(LayoutPreset.FullRect, LayoutPresetMode.KeepSize);
		}
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

		public sealed partial class Game() : PackDisplay
		{
			public override Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
				"Puzzles",
				container: new GridContainer { Name = "Grid", Columns = 5 }
					.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
			);
		}
		public sealed partial class Studio() : PackDisplay;
	}
	private sealed partial class GamePacks : PackDisplay
	{
		public override Labelled<Container> Puzzles { get; init; } = CreatePuzzles(
			"Puzzles",
			container: new GridContainer { Name = "Grid", Columns = 5 }
				.SizeFlags(horizontal: SizeFlags.Fill, vertical: SizeFlags.ExpandFill)
		);
	}
	private sealed partial class StudioPacks : PackDisplay;
	public partial class PuzzleDisplay : PanelContainer
	{
		public interface IPressed { T Pressed<T>(T display, SaveData data) where T : PuzzleDisplay; }
		public static PuzzleDisplay CreateGameDisplay(SaveData puzzle, Action pressed)
		{
			return new GamePuzzleDisplay(puzzle, pressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		}
		public static PuzzleDisplay CreateStudioDisplay(SaveData puzzle, Action pressed)
		{
			return new StudioPuzzleDisplay(puzzle, pressed)
			.SizeFlags(both: SizeFlags.ExpandFill);
		}
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
		static StyleBoxFlat Modify(StyleBoxFlat style)
		{
			style.SetCornerRadiusAll(0);
			style.SetContentMarginAll(40);
			return style;
		}

		public sealed partial class Game : PuzzleDisplay;
		public sealed partial class Studio : PuzzleDisplay;
	}
	public sealed partial class GamePuzzleDisplay(SaveData Puzzle, Action Pressed) : PuzzleDisplay
	{
		public override void _Ready()
		{
			base._Ready();
			Name = Puzzle.Name;

			Background.Color = Puzzle.CompletionColour;

			Button.Name = Puzzle.Name + " Button";
			Button.Text = Puzzle.IsComplete ? Puzzle.Name : string.Empty;
			Button.Icon = Puzzle.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
		}
	}
	public sealed partial class StudioPuzzleDisplay(SaveData Puzzle, Action Pressed) : PuzzleDisplay
	{
		public override void _Ready()
		{
			base._Ready();
			Name = Puzzle.Name;

			Background.Color = Puzzle.CompletionColour;

			Button.Name = Name + " Button";
			Button.Text = Name;
			Button.Icon = Puzzle.Expected.AsIcon(Core.Colours, 16);
			Button.Pressed += Pressed;
		}
	}
}

