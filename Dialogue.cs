using Godot;

namespace RSG;

using static Dialogue;

public static class DialogueBuilder
{
	public static Dialogues BuildDialogues(this Dialogues dialogues)
	{
		dialogues.SingleSpeaker(
			Name: Intro,
			Title: "Little Green Dude",
			(
				"hello, did you know... i'm quite green",
				Profile.LGD,
				Background.Background1
			),
			"if only I was blue, but this is the hand i've been dealt",
			"Any way, i'm going to go now...",
			"You didn't ask but im going to the 4th plane of existence, it's quite calming there",
			"You haven't heard of it?",
			"Well that's unfortunate for you I'm off..."
		);

		dialogues.SingleSpeaker(
			Name: CatOnThePath,
			Title: "Little Green Dude",
			(
				"hello, did you know... i'm quite green",
				Profile.LGD,
				Background.Background1
			),
			"if only I was blue, but this is the hand i've been dealt",
			(
				"what?",
				Background.Background2
			),
			"OH, a little cat has graced you with there presence"
		);
		return dialogues;
	}
}

public abstract record Dialogue
{
	public const string Intro = "intro", CatOnThePath = "catOnThePath";
	public readonly record struct Background(CompressedTexture2D Value)
	{
		public static Background Background1 { get; } = new(Dialogues.Resources.Background1);
		public static Background Background2 { get; } = new(Dialogues.Resources.Background2);
	}
	public readonly record struct Profile(CompressedTexture2D Value)
	{
		public static Profile LGD { get; } = new(Dialogues.Resources.Profile);
	}
	public readonly record struct Message(string Title, string Text);
	public readonly record struct Speech(ReadOnlyMemory<Message> Messages);
}
