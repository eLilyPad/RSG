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
	public static readonly Dialogues Instance = new();
	public static DialogueContainer Container => field ??= new DialogueContainer { Name = "Dialogue Container", Visible = false }
		.Preset(preset: Control.LayoutPreset.FullRect, resizeMode: Control.LayoutPresetMode.KeepSize, 30);
	public static DialogueResources Resources => field ??= Core.DialoguesPath.LoadOrCreateResource<DialogueResources>();
	public static IEnumerable<string> AvailableDialogues => Instance.Speeches.Keys.Except(
		Instance.Enabled.Where(pair => !pair.Value).Select(pair => pair.Key)
	);

	private static readonly CurrentDialogue Current = new();
	public static void Start(string name, bool? enable = null)
	{
		if (string.IsNullOrEmpty(name)) return;
		if (!Instance.Speeches.TryGetValue(name, out Speech speech)) return;
		(Current.Speech, Current.SpeechIndex, Current.Name) = (speech, 0, name);
		DisplayCurrent();
		if (enable is not bool enabled) return;
		Instance.Enabled[name] = enabled;
	}
	public static void Next()
	{
		if (!Container.Visible) { return; }
		Current.SpeechIndex++;
		DisplayCurrent();
	}

	private static void DisplayCurrent()
	{
		Container.Show();
		ReadOnlySpan<Message> messages = Current.Speech.Messages.Span;
		if (messages.Length <= Current.SpeechIndex)
		{
			Container.Hide();
			return;
		}
		Instance.Display(in messages[Current.SpeechIndex]);
	}

	private Dictionary<string, Speech> Speeches { get; } = [];
	private Dictionary<string, Extras> SpeechExtras { get; } = [];
	private Dictionary<string, bool> Enabled { get; } = [];
	private Dialogues() { }

	public void Enable(string name) => Enabled[name] = true;
	public void EnableAll()
	{
		foreach (string name in Speeches.Keys)
		{
			Enabled[name] = true;
		}
	}
	public Speech SingleSpeaker(in string Name, string Title, params ReadOnlySpan<SpeechTemplate> messages)
	{
		Assert(Instance is not null, "Instance is null");
		Assert(Name is not null, "Name is null");

		int index = 0;
		Message[] builtMessages = new Message[messages.Length];
		Extras extras = new();
		Enabled[Name] = false;

		foreach (SpeechTemplate message in messages)
		{
			message.Switch(AddText, AddBackground, AddProfile, AddAll);
			index++;
		}
		return Add(Name, new(Messages: builtMessages), extras);

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

	private Speech Add(string name, Speech speech, Extras extras)
	{
		SpeechExtras[name] = extras;
		return Speeches[name] = speech;
	}
	private void Display(in Message message)
	{
		Container.Message.Title.Text = message.Title;
		string text = Container.Message.Message.Text = message.Text;
		if (!SpeechExtras.TryGetValue(Current.Name, out Extras? extras)) return;
		if (extras.Backgrounds.TryGetValue(text, out CompressedTexture2D? background))
		{
			Container.Background.Texture = background;
		}
		if (extras.Profiles.TryGetValue(text, out CompressedTexture2D? profile))
		{
			Container.Profile.ProfileTexture.Texture = profile;
		}

	}
}
