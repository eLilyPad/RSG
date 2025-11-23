using Godot;

namespace RSG;

using static Audio;

public static class AudioExtensions
{
	public static class Errors
	{
		public const string
		MainLoop = "Main Loop is not SceneTree",
		PlayerNotFound = "Player has not been found",
		PlayerNotReady = "Player is not ready";
	}
	private record StreamPlayers
	{
		public Dictionary<Buses, AudioStreamPlayer> Players { get; } = Enum.GetValues<Buses>()
		.ToDictionary(elementSelector: Create);
		public OneOf<AudioStreamPlayer, Error<string>> Get(Buses bus)
		{
			if (Engine.GetMainLoop() is not SceneTree tree)
			{
				return new Error<string>(value: Errors.MainLoop);
			}
			if (!Players.TryGetValue(bus, out AudioStreamPlayer? player))
			{
				return new Error<string>(value: Errors.PlayerNotFound);
			}
			if (!player.IsInsideTree())
			{
				tree.Root.Add(player);
			}
			if (!player.IsNodeReady())
			{
				return new Error<string>(value: Errors.PlayerNotReady);
			}
			return player;
		}
		private static AudioStreamPlayer Create(Buses bus) => new()
		{
			Name = bus.GetName() + " Audio Player",
			Bus = bus.GetName(),
			Autoplay = false,
			VolumeDb = bus.Volume(),
			PitchScale = 1
		};
	}
	public static void Play(this Buses bus, AudioStream stream)
	{
		Players.Get(bus).Switch(Play, HandleError);
		void HandleError(Error<string> error) => GD.PushWarning("Unable to play sound: " + error.Value);
		void Play(AudioStreamPlayer player)
		{
			if (player.Playing) player.Stop();
			player.Stream = stream;
			player.Play();
		}
	}
	public static float Volume(this Buses bus, double? value = null)
	{
		if (value is double linear) AudioServer.SetBusVolumeDb(
			busIdx: (int)bus,
			volumeDb: Mathf.LinearToDb((float)linear)
		);
		return Mathf.DbToLinear(
			db: AudioServer.GetBusVolumeDb(busIdx: (int)bus)
		);
	}
	public static string GetName(this Buses bus) => bus switch
	{
		Buses.Music => "Music",
		Buses.SoundEffects => "Sound Effects",
		_ => "Master"
	};

	private static StreamPlayers Players { get; } = new();
}

public sealed partial class Audio : Resource
{
	public enum Buses { Master = 0, Music = 1, SoundEffects = 2 }
	public sealed partial class Container : ScrollContainer
	{
		public VolumeSlider Master { get; } = new VolumeSlider(bus: Buses.Master);
		public VolumeSlider SoundEffects { get; } = new VolumeSlider(bus: Buses.SoundEffects);
		public VolumeSlider Music { get; } = new VolumeSlider(bus: Buses.Music);
		public HBoxContainer Margin { get; } = new HBoxContainer()
		.Preset(LayoutPreset.TopRight, LayoutPresetMode.KeepSize, 30);
		public override void _Ready()
		{
			Name = "AudioContainer";
			this.Add(Margin.Add(Master, SoundEffects, Music));
		}
	}
	public sealed partial class VolumeSlider : BoxContainer
	{
		public RichTextLabel VolumeLabel { get; } = new() { CustomMinimumSize = new Vector2(x: 60, y: 0) };
		public RichTextLabel NameLabel { get; } = new() { CustomMinimumSize = new Vector2(x: 60, y: 0) };
		public VBoxContainer Margin { get; } = new VBoxContainer()
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize, 10);
		public AspectRatioContainer Window { get; } = new()
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsStretchRatio = 0.2f,
			StretchMode = AspectRatioContainer.StretchModeEnum.Fit,
			AlignmentHorizontal = AspectRatioContainer.AlignmentMode.End,
			AlignmentVertical = AspectRatioContainer.AlignmentMode.Begin
		};
		public HSlider Slider { get; } = new()
		{
			MaxValue = 2,
			MinValue = 0,
			Step = 0.01,
			Editable = true,
			Scrollable = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		private Buses Bus
		{
			init
			{
				Name = $"{NameLabel.Text = value.GetName()} - Volume Slider";
				Slider.ValueChanged += volume =>
				{
					VolumeLabel.Text = $"{Mathf.Floor((value.Volume(value: (float)volume)) * 100)}%";
				};
				Slider.Value = value.Volume();
			}
		}

		public VolumeSlider(Buses bus) => Bus = bus;
		public override void _Ready() => this.Add(
			Window.Add(Margin.Add(NameLabel, Slider, VolumeLabel))
		);
	}

	public const string
	DefaultBusName = "Master",
	SoundsPath = "res://Data/SoundEffects/Default.tres";

	public static Nonogram.SoundEffects NonogramSounds => field ??= SoundsPath.LoadOrCreateResource<Nonogram.SoundEffects>();

}
