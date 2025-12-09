using Godot;

namespace RSG;

public class DialogueBuilder
{

}

public sealed class Dialogues
{
	private class CurrentDialogue
	{
		public required DialogueContainer Container { get; init; }
		public Dialogue.Speech Speech
		{
			get; set
			{
				field = value;
				SpeechIndex = 0;
			}
		}
		public int SpeechIndex { get; private set; }

		public void Start()
		{
			GD.Print("Start");
			SpeechIndex = 0;
			ReadOnlySpan<Dialogue.Message> messages = Speech.Messages.Span;
			Display(messages[SpeechIndex]);
		}
		public void Next()
		{
			if (!Container.Visible) { return; }
			SpeechIndex++;
			ReadOnlySpan<Dialogue.Message> messages = Speech.Messages.Span;
			if (messages.Length <= SpeechIndex)
			{
				Container.Hide();
				return;
			}
			Display(messages[SpeechIndex]);
		}
		private void Display(Dialogue.Message message)
		{
			Container.Title.Text = message.Title;
			Container.Message.Text = message.Text;
		}
	}
	public static DialogueContainer Container { get; } = new DialogueContainer { Visible = true }
		.Preset(preset: Control.LayoutPreset.FullRect, resizeMode: Control.LayoutPresetMode.KeepSize);

	private static Dialogues Instance { get; } = new();
	private static CurrentDialogue Current => field ??= new() { Container = Container };

	public static void Start(string name)
	{
		if (!Instance.Speeches.TryGetValue(name, out Dialogue.Speech speech)) return;
		Current.Speech = speech;
		Current.Start();
	}
	public static void Next() => Current.Next();

	private Dictionary<string, Dialogue.Speech> Speeches { get; } = new()
	{
		[Dialogue.Intro] = Dialogue.SingleSpeaker("Title", "hello", "what?", "bye")
	};
	private Dialogues()
	{
		Current.Speech = Speeches[Dialogue.Intro];
	}
}

public abstract record Dialogue
{
	public const string Intro = "intro";
	public readonly record struct Message(string Title, string Text);
	public readonly record struct Speech(ReadOnlyMemory<Message> Messages);
	public static Speech SingleSpeaker(string Title, params ReadOnlySpan<string> textMessages)
	{
		Message[] messages = new Message[textMessages.Length];
		int index = 0;
		foreach (string text in textMessages)
		{
			messages[index] = new Message(Title, text);
			index++;
		}
		return new Speech(Messages: messages);
	}
}
