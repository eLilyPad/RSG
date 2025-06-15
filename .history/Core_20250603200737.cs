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
	public NonogramContainer Nonogram => field ??= NonogramContainer.Create(this);

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

public partial class NonogramPuzzle(Core core) : Resource
{
	public NonogramContainer Nonogram => field ??= NonogramContainer.Create(core);
}