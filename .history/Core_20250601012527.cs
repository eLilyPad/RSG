using Godot;

namespace RSG;

using UI;

public static class CoreExtensions { }
public static class SystemExtensions
{
	public static OneOf<T, NotFound> TryGet<T>(this T[] array, int index)
	{
		if (index < 0 || index >= array.Length) return new NotFound();
		return array[index];
	}
}

public interface IHavePenMode { Core.PenMode CurrentPenMode { get; } }
public interface IHaveColourPack { ColourPack Colours { get; } }

public sealed partial class Core : Node, IHavePenMode, IHaveColourPack
{
	public enum PenMode { Block, Fill }
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public PenMode CurrentPenMode { get; set; } = PenMode.Block;
	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public MainMenu Menu => field ??= new MainMenu { Colours = Colours };
	public Nonogram Nonogram => field ??= Nonogram.Create(this);

	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(
			//Menu, 
			Nonogram
		);
	}
}
