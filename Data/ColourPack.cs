using Godot;

namespace RSG;

using Nonogram;
using Minesweeper;

using Mode = Nonogram.Display.TileMode;

public static class ColoursPackExtensions
{
	public static TCore SetColours<TCore, TNonogram>(this TCore config)
	where TCore : IHaveColours<Nonogram.IColours>, IHaveCurrent<TNonogram>
	where TNonogram : NonogramContainer.IHave, IAlternate, SaveData.IHave, IDisplayType, ITiles<NodePool<Vector2I, Nonogram.Tile, TNonogram>, TNonogram>
	{
		config.Nonogram.SetColours(config.Colours);
		return config;
	}
	public static TCurrent SetColours<TCurrent>(this TCurrent config, Nonogram.IColours? value = null)
	where TCurrent :
		NonogramContainer.IHave,
		SaveData.IHave,
		IAlternate,
		IDisplayType,
		ITiles<NodePool<Vector2I, Nonogram.Tile, TCurrent>, TCurrent>
	{
		value ??= ColourPack.Default;
		config.UI.Background.ColorBackground.Color = config.Type.Background(value);
		config.UI.Display.Spacer.Timer.Background.Color = value.NonogramTimerBackground;
		foreach ((Vector2I position, Nonogram.Tile tile) in config.Tiles)
		{
			tile.SetColours(position, config, value);
		}
		return config;
	}
	public static Nonogram.Tile SetColours<T>(this Nonogram.Tile tile, Vector2I position, T config, Nonogram.IColours? value = null)
	where T : SaveData.IHave, IAlternate
	{
		Mode mode = config.Puzzle.States.GetValueOrDefault(position);
		return tile.SetColours(mode, config.IsAlternative(position), value);
	}
	public static Nonogram.Tile SetColours(this Nonogram.Tile tile, Mode mode, bool isAlternative, Nonogram.IColours? value = null)
	{
		value ??= ColourPack.Default;

		//mode ??= tile.Mode;
		Color tileColour = value.NonogramTileBackground(mode, alternative: isAlternative);
		Color lockedColour = value.NonogramLockedBorder(mode);
		tile.Button.OverrideStyle(modify: (StyleBoxFlat style) =>
		{
			style.BorderColor = lockedColour;
			style.BgColor = tileColour;
			return style;
		});
		tile.Button.OverrideStyle(name: "hover", modify: (StyleBoxFlat style) =>
		{
			style.BgColor = tileColour;
			return style;
		});
		return tile;
	}
}

public interface IHaveColours<T> { T Colours { get; } }
public sealed partial class ColourPack : Resource, Nonogram.IColours, Minesweeper.IColours
{
	public static ColourPack Default { get; } = new ColourPack();

	[Export] public Color MainMenuBackground { get; private set; } = Colors.Black;
	[Export] public Color MainMenuLevelsBackground { get; private set; } = Colors.DimGray;
	[Export] public Color MainMenuDialoguesBackground { get; private set; } = Colors.DimGray;

	[Export] public Color NonogramBackground { get; private set; } = Colors.DarkOliveGreen;
	[Export] public Color NonogramPaintBackground { get; private set; } = Colors.Aqua;
	[Export] public Color NonogramFilledBorder { get; private set; } = Colors.DarkBlue;
	[Export] public Color NonogramBlockedBorder { get; private set; } = Colors.DarkSeaGreen;
	[Export] public Color NonogramTimerBackground { get; private set; } = Colors.Burlywood;
	[Export] public Color NonogramHintBackground2 { get; private set; } = Colors.BlanchedAlmond;
	[Export] public Color NonogramHintBackground1 { get; private set; } = Colors.FloralWhite;
	[Export] public Color NonogramTileBackground2 { get; private set; } = Colors.BlanchedAlmond;
	[Export] public Color NonogramTileBackground1 { get; private set; } = Colors.FloralWhite;
	[Export] public Color NonogramTileBackgroundFilled { get; private set; } = Colors.Gold;

	[Export] public Color MinesweeperBombBackground { get; private set; } = Colors.Black;
	[Export] public Color MinesweeperEmptyBackground { get; private set; } = Colors.White;
	[Export] public Color MinesweeperCoveredBackground { get; private set; } = Colors.Beige;
	[Export] public Color MinesweeperBackground { get; private set; } = Colors.DarkSeaGreen;
}