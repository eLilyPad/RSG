using Godot;

namespace RSG;

using static Audio;

public static class AudioExtensions
{
	public static class Errors
	{
		public const string
		MainLoop = "Main Loop is not SceneTree",
		RootNotReady = "SceneTree's root is not ready",
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
			if (!tree.Root.IsNodeReady())
			{
				return new Error<string>(value: Errors.RootNotReady);
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
			if (!player.IsNodeReady()) return;
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
		Buses.Master => "Master",
		_ => throw new Exception("Bus enum value has no assigned name.")
	};
	public static AudioStream GetVolumeClickedAudio(this Buses bus) => bus switch
	{
		Buses.Music => NonogramSounds.TileClicked,
		Buses.SoundEffects => NonogramSounds.TileClicked,
		Buses.Master => NonogramSounds.TileClicked,
		_ => throw new Exception("Bus enum value has no assigned audio stream.")
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
		public VBoxContainer Margin { get; } = new VBoxContainer()
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill)
		.Preset(preset: LayoutPreset.TopRight, resizeMode: LayoutPresetMode.KeepSize, 30);
		public override void _Ready()
		{
			Name = "AudioContainer";
			this.Add(Margin.Add(Master, SoundEffects, Music));
		}
	}
	public sealed partial class VolumeSlider : HBoxContainer
	{
		public RichTextLabel VolumeLabel { get; } = new RichTextLabel { FitContent = true }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public RichTextLabel NameLabel { get; } = new RichTextLabel { FitContent = true }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public BoxContainer Margin { get; } = new HBoxContainer()
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.ExpandFill);
		public HSlider Slider { get; } = new()
		{
			MaxValue = 1,
			MinValue = 0,
			Step = 0.01,
			Value = 0.4,
			Editable = true,
			Scrollable = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
		};
		public VolumeSlider(Buses bus)
		{
			Name = (NameLabel.Text = bus.GetName()) + " - Volume Slider";
			ChangeVolume(volume: bus.Volume());

			this.Add(
				Margin.Add(NameLabel, Slider, VolumeLabel)
			);

			Slider.ValueChanged += ChangeVolume;

			void ChangeVolume(double volume)
			{
				bus.Play(bus.GetVolumeClickedAudio());
				Slider.Value = volume;
				VolumeLabel.Text = $"{Mathf.Floor((bus.Volume(value: volume)) * 100)}%";
			}
		}
	}

	public const string
	DefaultBusName = "Master",
	SoundsPath = "res://Data/SoundEffects/Default.tres";

	public static Nonogram.SoundEffects NonogramSounds => field ??= SoundsPath.LoadOrCreateResource<Nonogram.SoundEffects>();

}
