using Godot;

namespace RSG;

using UI;

public static class CoreExtensions { }

public interface IHavePenMode { Nonogram.PenMode CurrentPenMode { get; } }
public interface IHaveCore { Core Core { get; init; } }


public sealed partial class Core : Node
{
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public MainMenu Menu => field ??= new MainMenu { Colours = Colours };
	public Nonogram Nonogram => field ??= new(Colours);
	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Nonogram);
	}
}

public partial class Nonogram(ColourPack Colours) : Node, IHavePenMode, TilesContainer.IHandleTiles
{
	public enum Type { Game, Painter }
	public enum PenMode { Block, Fill }

	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
	public DisplaySettings Settings
	{
		get; set
		{
			field = value;
			GameDisplay.Free();
			PainterDisplay.Free();
			(GameDisplay, PainterDisplay) = (null, null);
			this.Add(GameDisplay, PainterDisplay);
		}
	}
	public NonogramContainer GameDisplay
	{
		get => field ??= NonogramContainer.Create(this, Colours, Settings);
		private set;
	}
	public NonogramContainer PainterDisplay
	{
		get => field ??= NonogramContainer.PainterDisplay(this, Colours, Settings);
		private set;
	}

	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(GameDisplay, PainterDisplay);
	}
	public void OnButtonPressed(Vector2I position, Button button)
	{
		(this as TilesContainer.IHandleButtonPress).OnButtonPressed(CurrentPenMode, position, button);
	}

	public readonly record struct DisplaySettings(int Length = 5, int Scale = 40, int Margin = 150)
	{
		public readonly Vector2I Size => Vector2I.One * Length;
		public readonly Vector2I TileSize => Size * Scale;
		public readonly Vector2I BackgroundSize => Size * (Scale + 5);
	}
}