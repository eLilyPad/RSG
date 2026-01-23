using Godot;

namespace RSG.Nonogram;

public sealed partial class SettingsMenuContainer : ScrollContainer
{
	public interface IChangeSettings
	{
		void ToggledLockFilledTiles(bool toggled);
		void ToggledLockBlockedTiles(bool toggled);
		void ToggledBlockCompleteLines(bool toggled);
	}
	public sealed partial class AutoCompletionContainer : VBoxContainer
	{
		public RichTextLabel Title { get; } = new RichTextLabel
		{
			Name = "Title",
			Text = "Assistance",
			FitContent = true,
		}.Preset(LayoutPreset.CenterTop, LayoutPresetMode.KeepWidth);
		public Labelled<CheckButton> LockFilledTiles { get; } = new Labelled<CheckButton>
		{
			Name = "LockFilled",
			Label = new RichTextLabel { Name = "Label", FitContent = true, Text = "Lock Correct Filled Tiles" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill),
			Value = new CheckButton { Name = "Check" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill)
		};
		public Labelled<CheckButton> LockBlockedTiles { get; } = new Labelled<CheckButton>
		{
			Name = "LockBlocked",
			Label = new RichTextLabel { Name = "Label", FitContent = true, Text = "Lock Correct Blocked Tiles" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill),
			Value = new CheckButton { Name = "Check" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill)
		};
		public Labelled<CheckButton> BlockCompleteLines { get; } = new Labelled<CheckButton>
		{
			Name = "BlockCompleted",
			Label = new RichTextLabel { Name = "Label", FitContent = true, Text = "Blocked Completed Lines" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill),
			Value = new CheckButton { Name = "Check" }
				.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill)
		};

		public override void _Ready() => this.Add(Title, LockFilledTiles, LockBlockedTiles, BlockCompleteLines);
	}

	public AutoCompletionContainer AutoCompletion { get; } = new AutoCompletionContainer { Name = "Auto Completion" }
		.SizeFlags(horizontal: SizeFlags.ExpandFill, vertical: SizeFlags.Fill);
	public IChangeSettings SettingsChanger
	{
		set
		{
			AutoCompletion.LockFilledTiles.Value.Toggled += value.ToggledLockFilledTiles;
			AutoCompletion.LockBlockedTiles.Value.Toggled += value.ToggledLockBlockedTiles;
			AutoCompletion.BlockCompleteLines.Value.Toggled += value.ToggledBlockCompleteLines;
			if (field is null)
			{
				field = value;
				return;
			}
			AutoCompletion.LockFilledTiles.Value.Toggled -= field.ToggledLockFilledTiles;
			AutoCompletion.LockBlockedTiles.Value.Toggled -= field.ToggledLockBlockedTiles;
			AutoCompletion.BlockCompleteLines.Value.Toggled -= field.ToggledBlockCompleteLines;
		}
	}

	public override void _Ready() => this.Add(AutoCompletion);
}
