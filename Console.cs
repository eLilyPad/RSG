namespace RSG;

public static class ConsoleExtensions
{
	public static void Run(
		this IDictionary<string, Dictionary<string, Console.Command>> modules,
		Console.CommandInput input
	)
	{
		Console.Command? command = null;
		_ = modules.TryGetValue(input.Prefix, value: out Dictionary<string, Console.Command>? module);
		_ = module?.TryGetValue(input.Phrase, value: out command);

		if (command is null) { return; }
		if (input.IsEmpty())
		{
			command.Default();
			return;
		}
		IEnumerable<string> flags = input.Flags.Where(command.Flags.ContainsKey);
		IEnumerable<KeyValuePair<string, object>> props = input.Properties
			.Where(k => command.Properties.ContainsKey(k.Key));
		foreach (string key in flags)
		{
			command.Flags[key]();
		}
		foreach ((string key, object obj) in props)
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
	public sealed record Command
	{
		public Action Default { get; init; } = static () => { };
		public Dictionary<string, Action<object>> Properties { get; init; } = [];
		public Dictionary<string, Action> Flags { get; init; } = [];
	}
	public readonly record struct CommandInput
	{
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

			static IReadOnlyList<string> flags(string s) => [
				.. s.Split(separator: ' ')
				.Where(predicate: a => a.StartsWith(FlagPrefix))
			];
			static IReadOnlyList<object> args(string s) => [
				.. s.Split(separator: ' ')
				.Where(static s => s is not FlagPrefix)
				.Select(static s => Convert.ChangeType(value: s, conversionType: s.ParseInput().GetType()))
			];
			static Dictionary<string, object> props(string s) => s.Split(' ')
				.Where(static a => a.StartsWith(PropertiesPrefix) && a.Contains('='))
				.Select(static a => a.Split('=', 2))
				.Where(static a => a.Length == 2)
				.Select(static a => new KeyValuePair<string, object>(key: a[0], value: a[1].ParseInput()))
				.ToDictionary();
		}
		private CommandInput(string prefix = "", string phrase = "")
		{
			Prefix = prefix;
			Phrase = phrase;
		}
		public readonly string Prefix, Phrase;
		public IReadOnlyList<string> Flags { get; init; } = [];
		public IReadOnlyList<object> Args { get; init; } = [];
		public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();

		public readonly bool IsEmpty() => Flags.Count == 0 && Args.Count == 0 && Properties.Count == 0;
	}

	public static void Add(string prefix, params ReadOnlySpan<(string, Command)> configs)
	{
		Instance.Modules[prefix] = [];
		foreach ((string input, Command command) in configs)
		{
			Instance.Modules[prefix][input] = command;
		}
	}

	public static Console Instance { get; } = new();

	public IEnumerable<string> Prefixes => [.. Modules.Keys];
	public Dictionary<string, Dictionary<string, Command>> Modules { private get; init; } = [];


	public void Run(CommandInput input) => Modules.Run(input);
	public void Submitted(string input) => Run(CommandInput.Parse(input));
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

