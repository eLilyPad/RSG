using Godot;
using static Godot.Control;

namespace RSG;

using UI;
using Nonogram;
using RSG.Minesweeper;

public sealed partial class Core : Node
{
	private sealed class CoreEventHandler(Core core) :
	PuzzleManager.IHaveEvents,
	MainMenu.IPress,
	MainMenu.IReceiveSignals
	{
		readonly List<PuzzleSelector.PackDisplay> _levelSelectorDisplays = [];
		readonly List<DialogueSelector.DialogueDisplay> _dialogueSelectorDisplays = [];
		public void PuzzleSelectorVisibilityChanged()
		{
			MainMenu menu = core.Container.Menu;
			PuzzleSelector puzzleSelector = menu.Levels;
			if (!puzzleSelector.Visible)
			{
				menu.Hide();
				return;
			}
			RefillPacks(
				root: puzzleSelector,
				parent: puzzleSelector.Puzzles.Value,
				nodes: _levelSelectorDisplays
			);
		}
		public void DialogueSelectorVisibilityChanged()
		{
			MainMenu menu = core.Container.Menu;
			DialogueSelector dialogueSelector = menu.Dialogues;
			if (!dialogueSelector.Visible)
			{
				menu.Hide();
				return;
			}
			Refill(
				root: dialogueSelector,
				parent: dialogueSelector.DisplayContainer.Value,
				nodes: _dialogueSelectorDisplays,
				configs: Dialogues.AvailableDialogues,
				create: DialogueSelector.DialogueDisplay.Create
			);
		}
		public void MenuVisibilityChanged()
		{
			MainMenu menu = core.Container.Menu;
			NonogramContainer nonogram = PuzzleManager.Current.UI;
			MinesweeperContainer minesweeper = core.Minesweeper.UI;
			if (!menu.Visible)
			{
				return;
			}
			if (nonogram.Visible)
			{
				nonogram.Hide();
			}
			if (minesweeper.Visible)
			{
				minesweeper.Hide();
			}
		}
		public void Completed(SaveData puzzle)
		{
			string dialogueName = puzzle.Expected.DialogueName;
			PuzzleManager.Current.UI.CompletionScreen.Show();
			Dialogues.Enable(dialogueName);
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = core.Container.Menu.Settings.Nonogram;
			Settings settings = PuzzleManager.Current.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
		public void PlayMinesweeperPressed()
		{
			core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			core.Minesweeper.UI.Show();
			core.Container.Menu.Hide();
		}
		public void PlayPressed()
		{
			PuzzleManager.CurrentPuzzle current = PuzzleManager.Current;
			switch (current)
			{
				case { PuzzleReady: true }:
					core.Container.Menu.Hide();
					PuzzleManager.Current.UI.Show();
					break;
				case { PuzzleReady: false }:
					core.Container.Menu.Levels.Show();
					core.Container.Menu.Show();
					break;
				default:
					break;
			}
			core.Container.Menu.Buttons.Hide();
		}
		public void LevelsPressed()
		{
			core.Container.Menu.Levels.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void DialoguesPressed()
		{
			core.Container.Menu.Dialogues.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void SettingsPressed()
		{
			core.Container.Menu.Settings.Show();
			core.Container.Menu.Buttons.Hide();
		}
		public void QuitPressed()
		{
			core.GetTree().Quit();
		}

		static void RefillPacks(CanvasItem root, Node parent, List<PuzzleSelector.PackDisplay> nodes)
		{
			IEnumerable<(string Name, IEnumerable<SaveData> Data)> configs = PuzzleManager.SelectorConfigs;
			Refill(root, parent, nodes, configs, create: PuzzleSelector.PackDisplay.Create);
		}
		static void Refill<TConfig, TNode>(
			CanvasItem root,
			Node parent,
			List<TNode> nodes,
			IEnumerable<TConfig> configs,
			Func<TConfig, CanvasItem, TNode> create
		)
		where TNode : Node
		{
			if (!root.Visible) return;
			parent.Remove(true, nodes);
			nodes.Clear();
			foreach (TConfig config in configs)
			{
				TNode node = create(config, root);
				parent.AddChild(node);
				nodes.Add(node);
			}
		}
	}
	public const string
	ColourPackPath = "res://Data/DefaultColours.tres",
	MinesweeperTexturesPath = "res://Data/MinesweeperTextures.tres",
	DialoguesPath = "res://Data/Dialogues.tres";
	public static ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public CoreUI Container => field ??= new CoreUI { Name = "Core UI", Colours = Colours }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.Minsize);

	private CoreEventHandler Events => field ??= new(this);
	private Manager Minesweeper
	{
		get
		{
			if (field is not null) return field;
			MinesweeperContainer ui = new MinesweeperContainer(Colours)
			{
				Name = "Minesweeper",
				Visible = false
			}.Preset(LayoutPreset.FullRect);
			Manager minesweeper = new() { UI = ui };

			Container.AddChild(ui);
			ui.Tiles.Provider = minesweeper;

			ui.Resized += () => ui.Background.Border.TextureBorder((Vector2I)ui.Size);

			return field = minesweeper;
		}
	}

	public override void _Ready()
	{
		Name = nameof(Core);

		this.Add(Container);
		Dialogues.Instance.BuildDialogues();

		CoreUI.ConnectSignals(Container);
		CoreUI.SetThemes(Container);
		Container.Menu.OverrideSignals(Events);

		Input.Bind(bindsContainer: Container.Menu.Settings.Input.InputsContainer,
			(Key.Escape, Container.EscapePressed, "Toggle Main Menu"),
			(Key.Backslash, CoreUI.ToggleConsole, "Toggle Console")
		);
		Console.Console.Add("/", ("quit", new Console.Console.Command { Default = () => GetTree().Quit() }),
			("nonogram", new Console.Console.Command
			{
				Default = () => Console.Console.Log("Current Display: " + PuzzleManager.Current.Type.AsName()),
				Flags = new()
				{
					["game"] = () => ChangeDisplayType(Display.Type.Game),
					["paint"] = () => ChangeDisplayType(Display.Type.Paint),
					["display"] = () => ChangeDisplayType(Display.Type.Display),
				}
			}
			)
		);

		PuzzleManager.Current.Type = Display.Type.Game;
		PuzzleManager.Current.EventHandler = Events;

		DisplayServer.WindowSetMode(DisplayServer.WindowMode.Fullscreen);

		static void ChangeDisplayType(Display.Type type)
		{
			PuzzleManager.Current.Type = type;
			Console.Console.Log($"Display changed too {type.AsName()}");
		}
	}
	public override void _Process(double delta)
	{
		PuzzleManager.Current.Timer.Tick(delta);
	}
	public override void _Input(InputEvent input)
	{
		if (!input.IsPressed()) return;
		if (Container.LoadingScreen.Visible)
		{
			Container.LoadingScreen.Hide();
			return;
		}
		if (input is InputEventMouseButton { Pressed: true })
		{
			Dialogues.Next(finished: DialogueFinished);
			return;
		}
		Input.RunEvent(input);

		void DialogueFinished()
		{
			Container.Menu.Show();
			Container.Menu.Buttons.Show();
		}
	}
}

