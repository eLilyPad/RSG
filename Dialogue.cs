using Godot;

namespace RSG;

using static Dialogue;

public static class DialogueBuilder
{
	public static Dialogues BuildDialogues(this Dialogues dialogues)
	{
		DialogueResources resources = Dialogues.Resources;
		dialogues.SingleSpeaker(
			Name: Intro,
			Title: "Little Green Dude",
			(
				"hello, did you know... i'm quite green",
				new Profile(resources.Profile),
				new Background(resources.Background1)
			),
			"if only I was blue, but this is the hand i've been dealt",
			(
				"what?",
				new Background(resources.Background2)
			),
			"OH, a little cat has graced you with there presence",
			"Any way, i'm going to go now...",
			"You didn't ask but im going to the 4th plane of existence, it's quite calming there",
			"You haven't heard of it?",
			"Well that's unfortunate for you I'm off..."
		);
		return dialogues;
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
