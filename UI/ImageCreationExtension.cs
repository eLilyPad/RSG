using Godot;

namespace RSG.UI;

public static class ImageCreationExtension
{
	public static void DrawLines(this TextureRect rect, Vector2I size, float space = 173f)
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
		rect.Texture = ImageTexture.CreateFromImage(image);
	}
	public static void DrawBorder(this TextureRect rect, Vector2I size, int pixelSize = 5, Color? borderColour = null)
	{
		Color color = borderColour ?? Colors.DarkGray;
		Image image = Image.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8);
		foreach (Vector2I position in size.GridRange())
		{
			(int x, int y) = position;
			if (!size.IsInBorder(position, thickness: pixelSize)) continue;
			image.SetPixel(x, y, color, pixelSize);
			if (size.IsCorner(position))
			{
				image.SetPixel(x, y, color, pixelSize * pixelSize);
			}
		}
		rect.Texture = ImageTexture.CreateFromImage(image);
	}
}
