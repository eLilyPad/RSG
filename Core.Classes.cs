using Godot;


namespace RSG;

using UI;
using Nonogram;
using Minesweeper;
using Dialogue;

// SubClasses are defined in separate partial class files for readability
public sealed partial class Core : Node
{
	private sealed class MenuHandler(Core Core) : MainMenu.IPress, MainMenu.IReceiveSignals
	{
		public void StudioPuzzleSelectorVisibilityChanged()
		{
			var root = PuzzleManager.Current.UI.Studio;
			if (!root.Visible) return;
			Core.StudioDisplays.Load(PuzzleManager.SelectorConfigs);
		}
		public void PuzzleSelectorVisibilityChanged()
		{
			var menu = Core.Container.Menu;
			var selector = menu.Levels;
			if (!selector.Visible)
			{
				menu.Hide();
				return;
			}
			Core.LevelDisplays.Load(PuzzleManager.SelectorConfigs);
		}
		public void DialogueSelectorVisibilityChanged()
		{
			var menu = Core.Container.Menu;
			var selector = menu.Dialogues;
			var container = selector.DisplayContainer.Value;

			if (!selector.Visible)
			{
				menu.Hide();
				return;
			}

			var availableDialogues = Dialogues.AvailableDialogues;
			bool hasDialogues = availableDialogues.Any();

			container.Remove(true, Core._dialogueSelectorDisplays);
			Core._dialogueSelectorDisplays.Clear();
			selector.EmptyDialoguesNotification.Visible = !hasDialogues;

			if (!hasDialogues) return;

			foreach (var config in availableDialogues)
			{
				var display = DialogueSelector.DialogueDisplay.Create(config, selector);
				container.AddChild(display);
				Core._dialogueSelectorDisplays.Add(display);
			}
		}
		public void LevelsPressed() => Core.Container.Menu.Levels.Show();
		public void DialoguesPressed() => Core.Container.Menu.Dialogues.Show();
		public void SettingsPressed() => Core.Container.Menu.Settings.Show();
		public void QuitPressed() => Core.GetTree().Quit();
		public void MenuVisibilityChanged()
		{
			NonogramContainer nonogram = PuzzleManager.Current.UI;
			MinesweeperContainer minesweeper = Core.Minesweeper.UI;
			if (!Core.Container.Menu.Visible) { return; }
			if (nonogram.Visible) { nonogram.Hide(); }
			if (minesweeper.Visible) { minesweeper.Hide(); }
		}
		public void PlayMinesweeperPressed()
		{
			Core.Minesweeper.Puzzle = Manager.Data.CreateRandom(10);
			Core.Minesweeper.UI.Show();
			Core.Container.Menu.Hide();
		}
		public void PlayPressed()
		{
			CurrentPuzzle current = PuzzleManager.Current;
			var menu = Core.Container.Menu;
			switch (current)
			{
				case { Type: Display.Type.Studio }:
					current.Type = Display.Type.Game;
					menu.Levels.Show();
					menu.Show();
					break;
				case { PuzzleReady: true }:
					menu.Hide();
					current.UI.Show();
					break;
				case { PuzzleReady: false }:
					menu.Levels.Show();
					menu.Show();
					break;
				default:
					break;
			}
			Core.Container.Menu.Buttons.Hide();
		}
		public void OpenStudioPressed()
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Type = Display.Type.Studio;
			current.UI.Show();
			Core.Container.Menu.Hide();
		}
	}
	private sealed class SettingsModifier(Core Core) : SettingsMenuContainer.IChangeSettings, PuzzleManager.IChangeWithSettings
	{
		public void ToggledLockFilledTiles(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedFilledTiles = toggled };
		}
		public void ToggledLockBlockedTiles(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LockCompletedBlockedTiles = toggled };
		}
		public void ToggledBlockCompleteLines(bool toggled)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			current.Settings = current.Settings with { LineCompleteBlockRest = toggled };
		}
		public void SettingsChanged()
		{
			SettingsMenuContainer menu = Core.Container.Menu.Settings.Nonogram;
			Settings settings = PuzzleManager.Current.Settings;

			menu.AutoCompletion.LockFilledTiles.Value.ButtonPressed = settings.LockCompletedFilledTiles;
			menu.AutoCompletion.LockBlockedTiles.Value.ButtonPressed = settings.LockCompletedBlockedTiles;
			menu.AutoCompletion.BlockCompleteLines.Value.ButtonPressed = settings.LineCompleteBlockRest;
		}
	}
	private sealed class GamesHandler(Core Core) : IHandleEvents, ISaveListener
	{
		public void Failed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;

			completionScreen.Show();
			completionScreen.Value.TitleText = "Game Over";
		}
		public void Completed(Manager.Data data)
		{
			Backgrounded<MinesweeperContainer.CompletedScreen> completionScreen = Core.Minesweeper.UI.CompletionScreen;

			completionScreen.Show();
			completionScreen.Value.TitleText = "Mines Located!";
		}
		public void PuzzleTilesChanged(Vector2I position)
		{
			CurrentPuzzle current = PuzzleManager.Current;
			Hints hints = current.UI.Hints;

			hints.Refresh();
			current.RefreshCurrentStudioIcon(
				colours: Colours,
				displays: Core._studioPuzzleSelectorDisplays
			);
		}
		public void SaveTilesChanged(Vector2I position)
		{
			Display.Type type = PuzzleManager.Current.Type;
			Nonogram.Tile.Pool tiles = PuzzleManager.Current.UI.Tiles;

			if (type is not Display.Type.Game) return;
			tiles.TryLock(position);
		}
	}
}

