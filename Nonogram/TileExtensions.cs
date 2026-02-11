using Godot;

namespace RSG.Nonogram;

using static Display;

public interface ITiles<T, TConfig> where T : NodePool<Vector2I, Tile, TConfig> { T Tiles { get; } }
public static class TileExtensions
{
	public static TConfig SetTileHovering<TPool, TConfig>(this TConfig config, Vector2I position, bool value)
	where TPool : NodePool<Vector2I, Tile, TConfig>
	where TConfig : ITiles<TPool, TConfig>
	{
		IEnumerable<KeyValuePair<Vector2I, Tile>> tiles = config.Tiles.AllInLines(position);
		foreach ((Vector2I _, Tile tile) in tiles)
		{
			tile.Button.SetHovering(hovering: value);
		}
		return config;
	}
	public static Button SetHovering(this Button tile, bool hovering)
	{
		tile.Scale = Vector2.One * (hovering ? .9f : 1);
		return tile;
	}
	public static Button SetLocked(this Button tile, bool locked)
	{
		tile.OverrideStyle((StyleBoxFlat style) =>
		{
			style.SetBorderWidthAll(locked ? 2 : 0);
			return style;
		});
		return tile;
	}
	public static Tile SetDisplay<T, TPool>(this T config, Vector2I position, Tile? tile = null)
	where T : SaveData.IHave, Tile.ILocker, ITiles<TPool, T>
	where TPool : NodePool<Vector2I, Tile, T>
	{
		Tile backup = config.Tiles.GetOrCreate(position, config);
		tile ??= backup;
		Assert(tile == backup);
		return tile.SetDisplay(position, config);
	}
	public static Tile SetDisplay<T>(this Tile tile, Vector2I position, T config)
	where T : SaveData.IHave, Tile.ILocker
	{
		const TileMode defaultValue = TileMode.Clear;
		TileMode tileMode = config.Puzzle.States.GetValueOrDefault(position, defaultValue);
		tile.SetColours(Core.Colours);
		tile.Locked = config.ShouldLock(position);
		Assert(tile.Mode == tileMode);
		return tile;
	}
	public static Tile SetGridPosition(this Tile tile, Vector2I position, in int chunkSize)
	{
		(int x, int y) = position;
		tile.IsAlternative = (x / chunkSize + y / chunkSize) % 2 == 0;
		tile.Name = $"Tile (X: {x}, Y: {y})";
		return tile;
	}
}
public static class TileModeExtensions
{
	public static bool IsEmpty(this TileMode mode) => mode is TileMode.NULL or TileMode.Clear;
	public static bool ShouldIgnore(this TileMode expected, TileMode current, TileMode newValue) =>
		expected == current
		&& !(newValue is TileMode.Blocked && current is TileMode.Clear);
	public static bool IsCorrect<TKey>(this IImmutableDictionary<TKey, TileMode> tiles, TKey position, TileMode current)
	{
		if (!tiles.TryGetValue(position, out TileMode expected)) return false;
		return current.IsCorrectMode(expected);
	}
	public static bool IsCorrectMode(this TileMode current, TileMode expected) => expected switch
	{
		TileMode.Filled when current is TileMode.Filled => true,
		TileMode.Clear when current is TileMode.Clear or TileMode.Blocked => true,
		_ => false
	};

	public static double ToDouble(this TileMode mode) => mode switch
	{
		TileMode.Blocked => 2,
		TileMode.Filled => 1,
		_ => 0,
	};
	public static TileMode Change(this TileMode input, TileMode currents) => input switch
	{
		TileMode.NULL => currents,
		TileMode mode when mode == currents => TileMode.Clear,
		TileMode mode => mode
	};
	public static TileMode ToTileMode(this int mode) => mode switch
	{
		2 => TileMode.Blocked,
		1 => TileMode.Filled,
		_ => 0,
	};
	public static TileMode FromText(this string mode) => mode switch
	{
		Tile.BlockText => TileMode.Blocked,
		Tile.FillText => TileMode.Filled,
		Tile.EmptyText => TileMode.Clear,
		_ => TileMode.NULL
	};
	public static void PlayAudio(this TileMode mode)
	{
		if (mode.AsAudioStream() is AudioStream stream) Audio.Buses.SoundEffects.Play(stream);
	}
	public static AudioStream? AsAudioStream(this TileMode mode) => mode switch
	{
		TileMode.Filled => Audio.NonogramSounds.FillTileClicked,
		TileMode.Blocked => Audio.NonogramSounds.BlockTileClicked,
		_ => null
	};
	public static string AsText<T>(this IImmutableDictionary<T, TileMode> modes, T position) where T : notnull
	{
		return modes.GetValueOrDefault(position, TileMode.Clear).AsText();
	}
	public static string AsText(this TileMode mode) => mode switch
	{
		TileMode.Blocked => Tile.BlockText,
		TileMode.Filled => Tile.FillText,
		_ => Tile.EmptyText,
	};
}
