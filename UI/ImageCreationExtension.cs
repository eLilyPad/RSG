using Godot;

namespace RSG.UI;

public static class ImageCreationExtension
{
	public static void TextureLines(this TextureRect rect, Vector2I size, float space = 173f)
	{
		Image image = Image
			.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8)
			.TextureLines(space);
		rect.Texture = ImageTexture.CreateFromImage(image);
	}
	public static void TextureBorder(this TextureRect rect, Vector2I size, int pixelSize = 5, Color? borderColour = null)
	{
		Image image = Image
			.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8)
			.TextureBorder(pixelSize, borderColour);
		rect.Texture = ImageTexture.CreateFromImage(image);
	}
	public static void TextureNoise(this TextureRect rect, Vector2I size, Func<float, Color> colour)
	{
		Image image = Image
			.CreateEmpty(size.X, size.Y, false, Image.Format.Rgba8)
			.TextureNoise(colour);
		rect.Texture = ImageTexture.CreateFromImage(image);
	}

	private static Image TextureBorder(this Image image, int pixelSize = 5, Color? borderColour = null)
	{
		Color color = borderColour ?? Colors.DarkGray;
		Vector2I size = image.GetSize();
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
		return image;
	}
	private static Image TextureLines(this Image image, float space = 173f)
	{
		const int shift = 2, lineThickness = 4;
		Vector2I size = image.GetSize();
		foreach ((int x, int y) in size.GridRange())
		{
			int shiftedX = x - shift, shiftedY = y - shift;
			if (x % space is not 0 && y % space is not 0) continue;
			if (x < lineThickness || y < lineThickness) continue;
			image.SetPixel(shiftedX, shiftedY, Colors.DarkGray, lineThickness);
		}
		return image;
	}
	private static Image TextureNoise(this Image image, Func<float, Color> colour)
	{
		const float threshold = .1f;

		FastNoiseLite noise = new()
		{
			FractalOctaves = 10,
			Seed = Random.Shared.Next(5),
		};
		foreach (Vector2I position in image.GetSize().GridRange())
		{
			(int x, int y) = position;
			float value = noise.GetNoise2Dv(position);
			image.SetPixel(x, y, colour(value), 1);
		}
		return image;
	}
}
