using System.Data;
using static Godot.Control;

namespace RSG;

public sealed record Console
{
	public sealed record Command
	{
		public Action Default { get; init; } = static () => { };
		public Dictionary<string, Action<object>> Properties { get; init; } = [];
		public Dictionary<string, Action> Flags { get; init; } = [];
	}
	public sealed class CommandInput
	{
		public const string FlagPrefix = "--";
		public const string PropertiesPrefix = @"p-";
		public static CommandInput Parse(string input)
		{
			if (string.IsNullOrWhiteSpace(input)) return new();
			ReadOnlySpan<char> span = input.AsSpan().Trim();

			int i = 0;
			while (IsValidPrefix(ref i, ref span)) i++;
			ReadOnlySpan<char> prefixSpan = span[..i];

			int startPhrase = i;
			while (IsValidPhrase(ref i, ref span)) i++;
			ReadOnlySpan<char> phraseSpan = span[startPhrase..i];

			CommandInput commandInput = new() { Prefix = prefixSpan.ToString(), Phrase = phraseSpan.ToString() };
			SpanReader reader = new(span[i..span.Length]);
			while (reader.TryReadToken(out ReadOnlySpan<char> token))
			{
				ProcessToken(token, commandInput);
			}
			return commandInput;

			//return input.Split(separator: ' ', count: 3) switch
			//{
			//	[string Prefix, string Phrase, string rest] => new CommandInput(Prefix, Phrase)
			//	{
			//		Flags = flags(rest),
			//		Properties = props(rest),
			//		Args = args(rest)
			//	},
			//	[string Prefix, string Phrase] => new CommandInput(Prefix, Phrase),
			//	[string Prefix] => new CommandInput(Prefix),
			//	_ => new CommandInput()
			//};

			//static IReadOnlyList<string> flags(string s) => [
			//	.. s.Split(separator: ' ')
			//	.Where(predicate: a => a.StartsWith(FlagPrefix))
			//	.Select(selector: a => a[FlagPrefix.Length..])
			//];
			//static IReadOnlyList<object> args(string s) => [
			//	.. s.Split(separator: ' ')
			//	.Where(static s => s is not FlagPrefix)
			//	.Select(static s => Convert.ChangeType(value: s, conversionType: s.ParseInput().GetType()))
			//];
			//static Dictionary<string, object> props(string s) => s.Split(' ')
			//	.Where(static a => a.StartsWith(PropertiesPrefix) && a.Contains('='))
			//	.Select(static a => a[PropertiesPrefix.Length..].Split('=', 2))
			//	.Where(static a => a.Length == 2)
			//	.Select(static a => new KeyValuePair<string, object>(key: a[0], value: a[1].ParseInput()))
			//	.ToDictionary();

			static void ProcessToken(ReadOnlySpan<char> token, CommandInput result)
			{
				// Flag
				if (token.StartsWith(FlagPrefix))
				{
					string flag = token[FlagPrefix.Length..].ToString();
					result.Flags.Add(flag);
					Godot.GD.Print(flag);
					return;
				}

				// Property
				if (token.StartsWith(PropertiesPrefix))
				{
					int eq = token.IndexOf('=');
					if (eq > PropertiesPrefix.Length)
					{
						string key = token[PropertiesPrefix.Length..eq].ToString();
						object value = token[(eq + 1)..].ToString().ParseInput();

						result.Properties[key] = value;
						return;
					}
				}

				// Argument
				result.Args.Add(token.ToString().ParseInput());
			}
		}
		public static bool IsValidPrefix(ref int i, ref ReadOnlySpan<char> value)
		{
			return (uint)i < (uint)value.Length && !char.IsLetterOrDigit(value[i]);
		}
		public static bool IsValidPhrase(ref int i, ref ReadOnlySpan<char> value)
		{
			return (uint)i < (uint)value.Length && !char.IsWhiteSpace(value[i]);
		}
		public string Prefix { get; init; } = "";
		public string Phrase { get; init; } = "";
		public List<string> Flags { get; init; } = [];
		public List<object> Args { get; init; } = [];
		public Dictionary<string, object> Properties { get; init; } = [];

		private CommandInput() { }

		public bool IsEmpty() => Flags.Count == 0 && Args.Count == 0 && Properties.Count == 0;
	}

	public static void Add(string prefix, params ReadOnlySpan<(string, Command)> configs)
	{
		int i = 0;
		ReadOnlySpan<char> span = prefix.AsSpan().Trim();
		while (CommandInput.IsValidPrefix(ref i, ref span)) i++;
		Assert(i == prefix.Length, "given prefix is not valid");
		Instance.Modules[prefix] = [];
		foreach ((string phrase, Command command) in configs)
		{
			Instance.Modules[prefix][phrase] = command;
		}
	}
	public static void Log(string value)
	{
		Container.Log.Label.Text += value + "\n"; ;
	}
	public static void GrabInputFocus(bool clearSuggestions = false)
	{
		if (clearSuggestions)
		{
			Container.Input.Line.Clear();
		}
		if (!Container.Input.Line.IsVisibleInTree()) return;
		Container.Input.Line.GrabFocus();
	}

	public static Console Instance { get; } = new();
	public static ConsoleContainer Container => field ??= new ConsoleContainer { Name = "Console", Visible = false }
		.Preset(preset: LayoutPreset.FullRect, resizeMode: LayoutPresetMode.KeepSize);

	public IEnumerable<string> Prefixes => [.. Modules.Keys];
	public Dictionary<string, Dictionary<string, Command>> Modules { private get; init; } = [];

	public void Submitted(string input)
	{
		if (input.Length == 0) return;
		Modules.Run(CommandInput.Parse(input), out string? response);
		Log(input);
		Godot.GD.Print(response);
		if (response is not null) Log(response);
		GrabInputFocus(clearSuggestions: true);
	}
	public IEnumerable<string> Suggestions(string input)
	{
		CommandInput commandInput = CommandInput.Parse(input);
		if (!Modules.TryGetValue(commandInput.Prefix, out Dictionary<string, Command>? module))
		{
			return Modules.Keys
				.Where(prefix => prefix.StartsWith(commandInput.Prefix, StringComparison.OrdinalIgnoreCase));
		}
		if (commandInput.Phrase is { Length: 0 })
		{
			return module.Keys
				.Where(key => key.StartsWith(commandInput.Phrase, StringComparison.OrdinalIgnoreCase));
		}
		return [];
	}
}

