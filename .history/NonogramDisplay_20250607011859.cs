using Godot;

namespace RSG.UI;

using HintContainers = (BoxContainer Rows, BoxContainer Columns);

public static class NonogramExstensions
{
}

public abstract partial class NonogramDisplay : Container
{
	public const string BlockText = "X", FillText = "O", EmptyText = " ";

	public Dictionary<Vector2I, Button> Buttons { get; } = [];
	public Dictionary<Vector2I, RichTextLabel> Hints { get; } = [];

	public GridContainer Tiles { get; } = new() { Name = "Tiles" };
	public HintContainers HintContainers { get; } = (new(), new());
	public ColorRect BackgroundRect => field ??= Init(value: new Background
	{
		Name = "Background"
	});
	public Control Spacer => field ??= Init(value: new Control());
	public GridContainer Main => field ??= Init(value: new GridContainer());

	public override void _Ready()
	{
		Name = nameof(NonogramDisplay);
		(SizeFlagsHorizontal, SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		Init(HintContainers.Columns, HintContainers.Rows);

		this.Add(
			BackgroundRect,
			Main.Add(HintContainers.Columns, HintContainers.Rows, Tiles, Spacer)
		);

		Vector2I size = Vector2I.One * Tiles.Columns;
		foreach (Vector2I position in size.AsRange())
		{
			var button = Buttons[position] = Init(new Button
			{
				Name = $"Button {position}",
				Text = EmptyText
			});
			button.Pressed += () => OnTilePressed(position, button);
		}

		UpdateSettings();
	}

	public abstract void OnTilePressed(Vector2I position, Button button);
	public abstract void UpdateSettings();

	private void Init(params Span<Control> values)
	{
		foreach (var control in values)
		{
			_ = Init(value: control);
		}
	}
	private T Init<T>(T value) where T : Control
	{
		switch (value)
		{
			case Button tile:
				Tiles.AddChild(tile);
				break;
			case GridContainer tiles when tiles == Tiles:
				Main.AddChild(tiles);
				break;
			case GridContainer main when main == Main:
				(main.Name, main.Columns) = ("MainContainer", 2);
				main.SetAnchorsAndOffsetsPreset(
					preset: LayoutPreset.FullRect,
					resizeMode: LayoutPresetMode.KeepSize
				);
				AddChild(main);
				break;
			case ColorRect background when background == BackgroundRect:
				background.Name = "Background";
				AddChild(background);
				break;
			case BoxContainer hints:
				hints.Name = hints switch
				{
					_ when hints == HintContainers.Rows => "RowHints",
					_ when hints == HintContainers.Columns => "ColumnsHints",
					_ => ""
				};
				Main.AddChild(hints);
				break;
			case Control spacer when spacer == Spacer:
				(spacer.Name, spacer.Size) = ("Spacer", Tiles.Size);
				Main.AddChild(spacer);
				break;
		}

		(value.SizeFlagsHorizontal, value.SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
		value.AnchorsAndOffsetsPreset(
			preset: LayoutPreset.FullRect,
			resizeMode: LayoutPresetMode.KeepSize
		);
		return value;
	}

	public sealed partial class Background : ColorRect
	{
		public override void _Ready()
		{
			Name = nameof(Background);
			(SizeFlagsHorizontal, SizeFlagsVertical) = (SizeFlags.ExpandFill, SizeFlags.ExpandFill);
			SetAnchorsAndOffsetsPreset(
				preset: LayoutPreset.FullRect,
				resizeMode: LayoutPresetMode.KeepSize
			);
		}
	}
}
