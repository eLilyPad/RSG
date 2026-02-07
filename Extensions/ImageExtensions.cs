using Godot;

namespace RSG.Extensions;

public static class ImageExtensions
{
	public static void SetPixel(this Image image, int x, int y, Color color, int size)
	{
		int half = size / 2;

		for (int dx = -half; dx <= half; dx++)
		{
			for (int dy = -half; dy <= half; dy++)
			{
				int px = x + dx;
				int py = y + dy;

				if (image.PixelInImage(px, py))
				{
					image.SetPixel(px, py, color);
				}
			}
		}
	}
	public static bool PixelInImage(this Image image, int x, int y)
	{
		return x >= 0 && y >= 0 && x < image.GetWidth() && y < image.GetHeight();
	}
}
