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
		public Speech Speech { get; internal set => (field, SpeechIndex) = (value, 0); }
		public int SpeechIndex { get; set; }
		public string Name { get; set; } = Intro;
	}
	private record Extras
	{
		public Dictionary<string, CompressedTexture2D> Backgrounds { get; } = [];
		public Dictionary<string, CompressedTexture2D> Profiles { get; } = [];
	}
	public static DialogueContainer Container => field ??= new DialogueContainer { Visible = false, Resources = Resources }
		.Preset(preset: Control.LayoutPreset.FullRect, resizeMode: Control.LayoutPresetMode.KeepSize);
	public static DialogueResources Resources => field ??= Core.DialoguesPath.LoadOrCreateResource<DialogueResources>();
	public static Dialogues Instance => field ??= new();

	private static CurrentDialogue Current => field ??= new();

	public static void Start(string name)
	{
		if (!Instance.Speeches.TryGetValue(name, out Speech speech)) return;
		(Current.Speech, Current.SpeechIndex, Current.Name) = (speech, 0, name);
		DisplayCurrent();
	}
	public static void Next()
	{
		if (!Container.Visible) { return; }
		Current.SpeechIndex++;
		DisplayCurrent();
	}
	public static IEnumerable<string> GetAvailableDialogues() => Instance.Speeches.Keys;

	private static void DisplayCurrent()
	{
		Container.Show();
		ReadOnlySpan<Message> messages = Current.Speech.Messages.Span;
		if (messages.Length <= Current.SpeechIndex)
		{
			Container.Hide();
			return;
		}
		Instance.Display(messages[Current.SpeechIndex]);
	}

	private Dictionary<string, Speech> Speeches { get; } = [];
	private Dictionary<string, Extras> SpeechExtras { get; } = [];
	private Dialogues() { }

	public Speech SingleSpeaker(string Name, string Title, params ReadOnlySpan<SpeechTemplate> messages)
	{
		Assert(Instance is not null, "Instance is null");
		Assert(Name is not null, "Name is null");

		int index = 0;
		Message[] builtMessages = new Message[messages.Length];
		Extras extras = new();

		foreach (SpeechTemplate message in messages)
		{
			message.Switch(AddText, AddBackground, AddProfile, AddAll);
			index++;
		}
		SpeechExtras[Name] = extras;
		return Speeches[Name] = new(Messages: builtMessages);

		void AddText(string text) => builtMessages[index] = new(Title, text);
		void AddAll((string Text, Profile Profile, Background Background) value)
		{
			AddText(value.Text);
			extras.Backgrounds[value.Text] = value.Background.Value;
			extras.Profiles[value.Text] = value.Profile.Value;
		}
		void AddProfile((string Text, Profile Profile) value)
		{
			AddText(value.Text);
			extras.Profiles[value.Text] = value.Profile.Value;
		}
		void AddBackground((string Text, Background Background) value)
		{
			AddText(value.Text);
			extras.Backgrounds[value.Text] = value.Background.Value;
		}
	}

	private void Display(Message message)
	{
		Container.Title.Text = message.Title;
		string text = Container.Message.Text = message.Text;
		if (!SpeechExtras.TryGetValue(Current.Name, out Extras? extras)) return;
		if (extras.Backgrounds.TryGetValue(text, out CompressedTexture2D? background))
		{
			Container.Background.Texture = background;
		}
		if (extras.Profiles.TryGetValue(text, out CompressedTexture2D? profile))
		{
			Container.Profile.Texture = profile;
		}

	}
}
