using Godot;

namespace RSG;

using UI;

public static class CoreExtensions { }

public interface IHavePenMode { Core.PenMode CurrentPenMode { get; } }
public interface IHaveColourPack { ColourPack Colours { get; } }

public sealed partial class Core : Node, IHavePenMode, IHaveColourPack, TilesContainer.IHandleButtonPress
{
	public enum PenMode { Block, Fill }
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
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

	public void OnButtonPressed(Vector2I position, Button button)
	{
		(this as TilesContainer.IHandleButtonPress).OnButtonPressed(CurrentPenMode, position, button);
	}
}

public partial class Nonogram(Core core) : Node
{
	public enum Type { Game, Painter }

	public DisplaySettings Settings { get; init; } = new();
	public NonogramContainer GameDisplay
	{
		get => field ??= NonogramContainer.Create(
			data: core,
			displaySettings: Settings
		);
		private set;
	}
	public NonogramPainterContainer PainterDisplay
	{
		get => field ??= NonogramPainterContainer.Create(
			data: core,
			Settings
		);
		private set;
	}
	public override void _Ready()
	{
		Name = nameof(Nonogram);
		this.Add(
			GameDisplay,
			PainterDisplay
		);
	}

	public readonly record struct DisplaySettings(int Length = 5, int Scale = 40, int Margin = 150)
	{
		public static implicit operator (int length, int scale, int margin)(DisplaySettings settings)
		{
			return (settings.Length, settings.Scale, settings.Margin);
		}
	}
}