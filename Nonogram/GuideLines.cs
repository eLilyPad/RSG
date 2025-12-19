using Godot;

namespace RSG.Nonogram;

public sealed partial class GuideLines : Container
{
	public ColorRect BackGround { get; } = new ColorRect { Color = Colors.AntiqueWhite with { A = 0.3f } }
		.Preset(LayoutPreset.FullRect);
	public TextureRect Lines { get; } = new TextureRect { Name = "Lines", ClipContents = true }.Preset(LayoutPreset.FullRect);
	//public Texture2D Lines { get; }
	public override void _Ready() => this.Add(
		BackGround,
		Lines
	);
	public void CreateLines(Vector2I size, float space = 173f)
	{
		const int shift = 2, lineThickness = 4;
		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		foreach ((int x, int y) in size.GridRange())
		{
			int shiftedX = x - shift, shiftedY = y - shift;
			if (x % space is not 0 && y % space is not 0) continue;
			if (x < lineThickness || y < lineThickness) continue;
			image.SetPixel(shiftedX, shiftedY, Colors.DarkGray, lineThickness);
		}
		Lines.Texture = ImageTexture.CreateFromImage(image);

	}
}