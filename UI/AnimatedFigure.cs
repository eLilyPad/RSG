using Godot;

namespace RSG.UI;

using static AnimatedFigure;
public static class JointExtensions
{
	public static T Create<T>(this T values, int size = 640) where T : IDictionary<Joints, Vector2>
	{
		int
		center = Mathf.RoundToInt(size * .5f),
		footHeight = Mathf.RoundToInt(size * .9f),
		legHeight = Mathf.RoundToInt(size * .5f),
		handHeight = Mathf.RoundToInt(size * .6f),
		shoulderHeight = Mathf.RoundToInt(size * .2f);

		values[Joints.Head] = new(center, size * .14f);
		values[Joints.Chest] = new(center, size * .25f);
		values[Joints.Hip] = new(center, center);
		values[Joints.LeftShoulder] = new(size * .7f, shoulderHeight);
		values[Joints.RightShoulder] = new(size * .3f, shoulderHeight);
		values[Joints.LeftHand] = new(size * .8f, handHeight);
		values[Joints.RightHand] = new(size * .2f, handHeight);
		values[Joints.LeftLeg] = new(size * .6f, legHeight);
		values[Joints.RightLeg] = new(size * .4f, legHeight);
		values[Joints.LeftFoot] = new(size * .6f, footHeight);
		values[Joints.RightFoot] = new(size * .4f, footHeight);

		return values;
	}
	public static IEnumerable<Joints> Connected(this Joints joint)
	{
		return joint switch
		{
			Joints.Head => [Joints.Chest],
			Joints.Chest => [Joints.Head, Joints.Hip, Joints.LeftShoulder, Joints.RightShoulder],
			Joints.LeftShoulder => [Joints.LeftHand],
			Joints.RightShoulder => [Joints.RightHand],
			Joints.Hip => [Joints.RightLeg, Joints.LeftLeg],
			Joints.LeftLeg => [Joints.LeftFoot],
			Joints.RightLeg => [Joints.RightFoot],
			_ => []
		};
	}
}

public sealed partial class AnimatedFigure : Sprite2D
{
	public enum Joints
	{
		Head,
		Chest,
		Hip,
		LeftShoulder,
		RightShoulder,
		LeftHand,
		RightHand,
		LeftLeg,
		RightLeg,
		LeftFoot,
		RightFoot
	}
	internal readonly record struct StepTarget(Vector2 Position, bool Left);
	private const int size = 640, stepDistance = 100;
	private Vector2 _lastPosition;
	private bool _isWalking;
	private readonly WalkingFigureFrameAnimator _animator = new(
		values: new Dictionary<Joints, Vector2>().Create(size)
	);
	public void StepLeft() => Position = Position with { X = Position.X - stepDistance };
	public void StepRight() => Position = Position with { X = Position.X + stepDistance };
	public override void _Ready()
	{
		_animator.GenerateTargets(maxWidth: GetViewportRect().Size.X);
		_animator.LastPosition = GlobalPosition;
	}
	public override void _Process(double delta)
	{
		_animator.LastPosition = GlobalPosition;
		if (!_animator.IsWalking) { return; }

		Image image = Image.CreateEmpty(size, size, false, Image.Format.Rgba8);
		Vector2 closestRightFootTarget = _animator.GetClosestTarget(GlobalPosition, joint: Joints.RightFoot);
		Vector2 closestLeftFootTarget = _animator.GetClosestTarget(GlobalPosition, joint: Joints.LeftFoot);
		_animator.Parts[Joints.RightFoot] = closestRightFootTarget;
		_animator.Parts[Joints.LeftFoot] = closestLeftFootTarget;

		foreach (Vector2 position in _animator.PixelsToDraw())
		{
			(float x, float y) = position;
			Color color = _animator.GetPixelColor(position);
			image.SetPixel((int)x, (int)y, color, 10);
		}

		Texture = ImageTexture.CreateFromImage(image);
	}
	public override void _Draw()
	{
		foreach ((Rect2 rect, Color color) in _animator.StepTargets())
		{
			DrawRect(rect, color);
		}
	}
}
