using Godot;

namespace RSG;

using UI;
using Nonogram;

public interface IHaveCore { Core Core { get; init; } }

public sealed partial class Core : Node
{
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public MainMenu Menu => field ??= new() { Colours = Colours };
	public NonogramContainer Nonogram => field ??= new() { Name = "Nonogram" };
	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Nonogram, Menu);

		foreach (var puzzle in PuzzleManager.GetSavedPuzzles())
		{
			//GD.Print($"Found saved puzzle: {puzzle.Expected.Name} of size {puzzle.Expected.Size}");
		}
	}
	public override void _Input(InputEvent input)
	{
		switch (input)
		{
			case InputEventKey { Keycode: Key.Escape, Echo: false, Pressed: true }:
				Menu.Step();
				break;
		}
	}
}

