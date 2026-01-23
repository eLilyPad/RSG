using Godot;

namespace RSG;

[GlobalClass, Tool]
public partial class MainMenuLandscape : Node3D
{
	private sealed class FoliagePool(MainMenuLandscape Landscape) : NodePool<Vector3, Sprite3D>
	{
		protected override Node Parent(Vector3 value) => Landscape.Ground;
		protected override Sprite3D Create(Vector3 key)
		{
			CompressedTexture2D texture = GD.Load<CompressedTexture2D>("res://Data/Images/Cacti.png");
			Sprite3D tree = new()
			{
				Name = "Cactus",
				Texture = texture,
				Transform = new Transform3D(Basis.Identity, key),
				Scale = Vector3.One * .1f,
				RegionEnabled = true,
				RegionRect = new Rect2(0, 0, 190, 330),
			};
			Landscape.Foliage.AddChild(tree, true);
			return tree;
		}
	}
	[ExportToolButton("ReadyNode")]
	private Callable Generate => Callable.From(Ready);

	public Camera3D Camera { get; } = new()
	{
		Name = "Camera",
		Current = true,
		Transform = new Transform3D(basis: Basis.FromEuler(new(-11, 0, 0)), origin: new(0, 5, 10)),
	};
	public DirectionalLight3D Light { get; } = new()
	{
		Name = "SunLight",
		Transform = new Transform3D(basis: Basis.FromEuler(new(-Mathf.Pi / 4, -Mathf.Pi / 4, 0)), origin: new(0, 10, 0)),
		LightEnergy = 1.0f,
	};
	public Node3D Ground { get; } = new Node3D { Name = "Ground", };
	public Node3D Foliage { get; } = new Node3D { Name = "Foliage", };
	public MeshInstance3D GroundMesh { get; } = new MeshInstance3D
	{
		Name = "GroundMesh",
		Mesh = new PlaneMesh { Size = new Vector2(50, 50), SubdivideDepth = 10, SubdivideWidth = 10, },
		MaterialOverride = new StandardMaterial3D
		{
			AlbedoColor = new Color(0.2f, 0.8f, 0.2f),
			AlbedoTexture = new NoiseTexture2D
			{
				Width = 256,
				Height = 256,
				Noise = new FastNoiseLite
				{
					Seed = 12345,
					Frequency = .09f,
					FractalLacunarity = 3,
					FractalGain = 0.7f,
					FractalWeightedStrength = .1f
				},
				ColorRamp = new Gradient
				{
					Colors = [new(.686f, .565f, .243f), Colors.White,],
					Offsets = [0.0f, 1.0f],
				}
			}
		}
	};


	[Export]
	private Transform3D LightTransform { get => Light.Transform; set => Light.Transform = value; }

	private FoliagePool Trees => field ??= new(Landscape: this);

	private void Ready()
	{
		if (!Engine.IsEditorHint()) return;
		Add(Camera, this);
		Add(Light, this);
		Add(Ground, this);
		Add(GroundMesh, Ground);
		CreateFoliage();

		void CreateFoliage()
		{
			const int cactiCount = 50;
			int i = 0;
			while (i < cactiCount)
			{
				Vector3 position = new(GD.RandRange(-20, 20), .133f, GD.RandRange(-20, 20));
				var node = Trees.GetOrCreate(position);
				if (node.Owner != this) node.Owner = this;
				i++;
			}
		}
		void Add(Node3D node, Node3D parent)
		{
			if (node.GetParent() != parent) parent.AddChild(node, true);
			if (node.Owner != this) node.Owner = this;
		}
	}
}