namespace RSG;

public static class ConsoleExtensions
{
	public static void Run(
		this IDictionary<Console.CommandKey, Dictionary<Console.CommandKey, Console.Command>> modules,
		Console.CommandInput input
	)
	{
		Console.Command? command = null;
		_ = modules.TryGetValue(input.Prefix, value: out Dictionary<Console.CommandKey, Console.Command>? module);
		_ = module?.TryGetValue(input.Phrase, value: out command);

		if (command is null) { return; }
		if (input.IsEmpty())
		{
			command.Default();
			return;
		}
		IEnumerable<Console.CommandKey> flags = input.Flags.Where(command.Flags.ContainsKey);
		IEnumerable<KeyValuePair<Console.CommandKey, object>> props = input.Properties
			.Where(k => command.Properties.ContainsKey(k.Key));
		foreach (Console.CommandKey key in flags)
		{
			command.Flags[key]();
		}
		foreach ((Console.CommandKey key, object obj) in props)
		{
			command.Properties[key](obj);
		}
	}
	public static object ParseInput(this string input)
	{
		return input switch
		{
			_ when int.TryParse(input, out int result) => result,
			_ when float.TryParse(input, out float result) => result,
			_ when char.TryParse(input, out char result) => result,
			_ when bool.TryParse(input, out bool result) => result,
			null => "",
			_ => input
		};
	}
}

public sealed record Console
{
	public sealed record Command(CommandKey Key)
	{
		public static Dictionary<CommandKey, Command> ForGame(Action quit)
		{
			return new Dictionary<CommandKey, Command>
			{
				["g"] = new(Key: "quit") { Default = quit },
				["s"] = new(Key: "audio") { },
			};
		}
		public Action Default { get; init; } = static () => { };
		public Dictionary<CommandKey, Action<object>> Properties { get; init; } = [];
		public Dictionary<CommandKey, Action> Flags { get; init; } = [];
	}
	public readonly record struct CommandKey(string ID = "")
	{
		public static implicit operator CommandKey(string Key) => new(Key);
	}
	public readonly record struct CommandInput
	{
		public static implicit operator CommandInput(string input) => Parse(input);
		public const string FlagPrefix = "--";
		public const string PropertiesPrefix = @"p-";
		public static CommandInput Parse(string input)
		{
			return input.Split(separator: ' ', count: 3) switch
			{
				[string Prefix, string Phrase, string rest] => new CommandInput(Prefix, Phrase)
				{
					Flags = flags(rest),
					Properties = props(rest),
					Args = args(rest)
				},
				[string Prefix, string Phrase] => new CommandInput(Prefix, Phrase),
				[string Prefix] => new CommandInput(Prefix),
				_ => new CommandInput()
			};

			static IReadOnlyList<CommandKey> flags(string s) => [
				.. s.Split(separator: ' ')
			.Where(predicate: a => a.StartsWith(FlagPrefix))
			];
			static IReadOnlyList<object> args(string s) => [
				.. s.Split(separator: ' ')
			.Where(static s => s is not FlagPrefix)
			.Select(static s => Convert.ChangeType(value: s, conversionType: s.ParseInput().GetType()))
			];
			static Dictionary<CommandKey, object> props(string s) => s.Split(' ')
				.Where(static a => a.StartsWith(PropertiesPrefix) && a.Contains('='))
				.Select(static a => a.Split('=', 2))
				.Where(static a => a.Length == 2)
				.Select(static a => new KeyValuePair<CommandKey, object>(key: a[0], value: a[1].ParseInput()))
				.ToDictionary();
		}
		private CommandInput(CommandKey prefix = new(), CommandKey phrase = new())
		{
			Prefix = prefix;
			Phrase = phrase;
		}
		public readonly CommandKey Prefix, Phrase;
		public IReadOnlyList<CommandKey> Flags { get; init; } = [];
		public IReadOnlyList<object> Args { get; init; } = [];
		public IReadOnlyDictionary<CommandKey, object> Properties { get; init; } = new Dictionary<CommandKey, object>();

		public readonly bool IsEmpty() => Flags.Count == 0 && Args.Count == 0 && Properties.Count == 0;
	}

	public static Console Instance { get; } = new();

	public Dictionary<CommandKey, Dictionary<CommandKey, Command>> Modules { private get; init; } = [];

	public void Run(CommandInput input) => Modules.Run(input);
	public void Submitted(string input) => Run(CommandInput.Parse(input));
	public IEnumerable<string> Suggestions(string input)
	{
		CommandInput commandInput = CommandInput.Parse(input);
		if (!Modules.TryGetValue(commandInput.Prefix, out Dictionary<CommandKey, Command>? module))
		{
			return Modules.Keys
				.Select(k => k.ID)
				.Where(k => k.StartsWith(commandInput.Prefix.ID, StringComparison.OrdinalIgnoreCase));
		}
		if (commandInput.Phrase.ID.Length == 0)
		{
			return module.Keys
				.Select(k => k.ID)
				.Where(k => k.StartsWith(commandInput.Phrase.ID, StringComparison.OrdinalIgnoreCase));
		}
		return [];
	}
}

