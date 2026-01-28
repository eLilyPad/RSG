using Godot;

namespace RSG.UI;

using static AnimatedFigure;

public sealed class WalkingFigureFrameAnimator(Dictionary<Joints, Vector2> values)
{
	public const float MinSpeed = .1f;
	public Dictionary<Joints, Vector2> Parts { get; } = values;
	public IImmutableDictionary<Joints, Vector2> DefaultParts { get; } = values.ToImmutableDictionary();

	public Vector2 LastPosition
	{
		private get; set
		{
			Vector2 velocity = field - value;
			float speed = velocity.Length();
			IsWalking = speed > MinSpeed;
			field = value;
		}
	}
	public bool IsWalking { get; private set; }

	private readonly List<StepTarget> _stepTargets = [];

	public void GenerateTargets(float maxWidth)
	{
		const int maxSteps = 10_000;
		float height = Parts[Joints.LeftFoot].Y;
		for (int step = 0; step < maxSteps; step++)
		{
			Vector2
			stepPosition = new(maxWidth - step * 20, height);
			_stepTargets.Add(new(Position: stepPosition, Left: step % 2 == 0));
		}
	}
	public Color GetPixelColor(Vector2 position) => Parts.ContainsValue(position) ? Colors.Black : Colors.SlateGray;

	public IEnumerable<(Rect2 rect, Color color)> StepTargets()
	{
		foreach ((int step, StepTarget target) in _stepTargets.Index())
		{
			Color color = step % 2 == 0 ? Colors.DarkRed : Colors.AliceBlue;
			Rect2 rect = new(target.Position, size: Vector2.One * 10);
			yield return (rect, color);
		}
	}
	public List<Vector2> PixelsToDraw()
	{
		List<Vector2> values = [];
		foreach ((Joints joint, Vector2 position) in Parts)
		{
			foreach (Joints connectedJoint in joint.Connected())
			{
				Vector2 to = Parts[connectedJoint];
				Vector2 midPoint = position.Lerp(to, .5f);
				values.Add(midPoint);
			}
			values.Add(position);
		}
		return values;
	}
	public Vector2 GetClosestTarget(Vector2 position, Joints joint)
	{
		Assert(joint is Joints.LeftFoot or Joints.RightFoot, "Only Foot Joints can get a target");
		Vector2 closestTarget = Vector2.Inf;
		position += Parts[joint];
		bool isLeft = joint is Joints.LeftFoot;
		foreach ((Vector2 nextPosition, bool left) in _stepTargets)
		{
			if (left != isLeft) continue;
			float prevDistance = closestTarget.DistanceTo(position);
			float distance = nextPosition.DistanceTo(position);
			if (distance < prevDistance)
			{
				closestTarget = nextPosition;
			}
		}
		closestTarget = Vector2.One * 2;
		return closestTarget;
	}
}
