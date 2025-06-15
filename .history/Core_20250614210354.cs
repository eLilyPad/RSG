using Godot;

namespace RSG;

using UI;

public interface IHaveCore { Core Core { get; init; } }

public sealed partial class Core : Node
{
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public MainMenu Menu => field ??= new MainMenu { Colours = Colours };
	public Nonogram Nonogram => field ??= new() { Colours = Colours };
	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Nonogram);
	}
}
