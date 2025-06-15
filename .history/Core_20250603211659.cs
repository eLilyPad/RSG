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
	public Nonogram Nonogram => field ??= new(this);
	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(
			//Menu, 
			Nonogram
		);
	}
}

public partial class Nonogram(Core core) : Node, IHaveCore, IHavePenMode, TilesContainer.IHandleButtonPress
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
			this.Add(GameDisplay, PainterDisplay);
		}
	}
	public NonogramContainer GameDisplay
	{
		get => field ??= NonogramContainer.Create(this, core.Colours, Settings);
		private set;
	}
	public NonogramPainterContainer PainterDisplay
	{
		get => field ??= NonogramPainterContainer.Create(this, core.Colours, Settings);
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
		public static implicit operator (int length, int scale, int margin)(DisplaySettings settings)
		{
			return (settings.Length, settings.Scale, settings.Margin);
		}
	}
}