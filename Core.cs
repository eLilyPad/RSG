using Godot;

namespace RSG;

using UI;

public interface IHaveCore { Core Core { get; init; } }

public sealed partial class Core : Node
{
	public const string ColourPackPath = "res://Data/DefaultColours.tres";

	public ColourPack Colours => field ??= ColourPackPath.LoadOrCreateResource<ColourPack>();
	public MainMenu Menu => field ??= new MainMenu { Colours = Colours, Visible = false };
	public Nonogram Nonogram => field ??= new() { Name = "Nonogram" };
	public override void _Ready()
	{
		Name = nameof(Core);
		this.Add(Nonogram, Menu);
	}
	public override void _Input(InputEvent input)
	{
		if (input is InputEventKey { Keycode: Key.Escape, Echo: false, Pressed: true })
		{
			if (Nonogram.LoadingMenu.TryHide()) { return; }
			Menu.Visible = !Menu.Visible;
		}
	}
}

public abstract class ShareableCode<T>
{
	public abstract T Decode(string value);
	public abstract string Encode(T value);
}
public abstract class Saver<T>
{
	public string Name { get; set; } = "";
	public abstract void Save(T value);
	public abstract OneOf<T, NotFound> Load();
}
