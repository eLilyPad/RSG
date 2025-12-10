using Godot;

namespace RSG;

using static Dialogue;
using SpeechTemplate = OneOf<
	string,
	(string text, Dialogue.Background background),
	(string text, Dialogue.Profile profile),
	(string text, Dialogue.Profile profile, Dialogue.Background background)
>;
public sealed class Dialogues
{
	private class CurrentDialogue
	{
		public required DialogueContainer Container { get; init; }
		public Speech Speech
		{
			get; set
			{
				field = value;
				SpeechIndex = 0;
			}
		}
		public int SpeechIndex { get; private set; }
		public string Name { get; internal set; } = Intro;

		public void Start()
		{
			Container.Show();
			SpeechIndex = 0;
			ReadOnlySpan<Message> messages = Speech.Messages.Span;
			Display(messages[SpeechIndex]);
		}
		public void Next()
		{
			if (!Container.Visible) { return; }
			SpeechIndex++;
			ReadOnlySpan<Message> messages = Speech.Messages.Span;
			if (messages.Length <= SpeechIndex)
			{
				Container.Hide();
				return;
			}
			Display(messages[SpeechIndex]);
		}
		private void Display(Message message)
		{
			Container.Title.Text = message.Title;
			Container.Message.Text = message.Text;
			if (!Instance.SpeechExtras.TryGetValue(Name, out SpeechExtra? extras)) return;
			if (extras.Backgrounds.TryGetValue(message.Text, out CompressedTexture2D? background))
			{
				Container.Background.Texture = background;
			}
			if (extras.Profiles.TryGetValue(message.Text, out CompressedTexture2D? profile))
			{
				Container.Profile.Texture = profile;
			}

		}
	}
	internal record SpeechExtra
	{
		public Dictionary<string, CompressedTexture2D> Backgrounds { get; } = [];
		public Dictionary<string, CompressedTexture2D> Profiles { get; } = [];
	}
	public static DialogueContainer Container => field ??= new DialogueContainer
	{
		Visible = true,
		Resources = DialogueResources
	}
		.Preset(preset: Control.LayoutPreset.FullRect, resizeMode: Control.LayoutPresetMode.KeepSize);
	public static DialogueResources DialogueResources => field ??= Core.DialoguesPath.LoadOrCreateResource<DialogueResources>();

	public static Dialogues Instance => field ??= new();
	private static CurrentDialogue Current => field ??= new() { Container = Container };

	public static void Start(string name)
	{
		if (!Instance.Speeches.TryGetValue(name, out Speech speech)) return;
		Current.Speech = speech;
		Current.Name = name;
		Current.Start();
	}
	public static void Next() => Current.Next();

	private Dictionary<string, Speech> Speeches { get; } = [];
	private Dictionary<string, SpeechExtra> SpeechExtras { get; } = [];
	private Dialogues()
	{
		SingleSpeaker(
			Name: Intro,
			Title: "Little Green Dude",
			(
				"hello, did you know... i'm quite green",
				new Profile(DialogueResources.Profile),
				new Background(DialogueResources.Background1)
			),
			"if only I was blue, but this is the hand i've been dealt",
			(
				"what?",
				new Background(DialogueResources.Background2)
			),
			"OH, a little cat has graced you with there presence",
			"Any way, i'm going to go now...",
			"You didn't ask but im going to the 4th plane of existence, it's quite calming there",
			"You haven't heard of it?",
			"Well that's unfortunate for you I'm off..."
		);
	}
	public Speech SingleSpeaker(
		string Name,
		string Title,
		params ReadOnlySpan<SpeechTemplate> textMessages
	)
	{
		Assert(Instance is not null, "Instance is null");
		Assert(Name is not null, "Name is null");

		int index = 0;
		Message[] messages = new Message[textMessages.Length];
		SpeechExtra extras = new();

		foreach (var message in textMessages)
		{
			message.Switch(AddText, AddBackground, AddProfile, AddAll);
			index++;
		}
		SpeechExtras[Name] = extras;
		return Speeches[Name] = new Speech(Messages: messages);

		void AddText(string text) => messages[index] = new Message(Title, text);
		void AddAll((string Text, Profile Profile, Background Background) value)
		{
			AddText(value.Text);
			extras.Backgrounds[value.Text] = value.Background.Value;
			extras.Profiles[value.Text] = value.Profile.Value;
			GD.Print("adding profile and background");
		}
		void AddProfile((string Text, Profile Profile) value)
		{
			AddText(value.Text);
			extras.Profiles[value.Text] = value.Profile.Value;
			GD.Print("adding profile");
		}
		void AddBackground((string Text, Background Background) value)
		{
			AddText(value.Text);
			extras.Backgrounds[value.Text] = value.Background.Value;
			GD.Print("adding background");
		}
	}
}

public abstract record Dialogue
{
	public const string Intro = "intro";
	public readonly record struct Background(CompressedTexture2D Value);
	public readonly record struct Profile(CompressedTexture2D Value);
	public readonly record struct Message(string Title, string Text);
	public readonly record struct Speech(ReadOnlyMemory<Message> Messages);
}
