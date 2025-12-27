using System.Text;

namespace RSG;

public static class ConsoleExtensions
{
	public static void Run(
		this IDictionary<string, Dictionary<string, Console.Command>> modules,
		Console.CommandInput input,
		out string? response
	)
	{
		response = null;
		if (!modules.TryGetValue(input.Prefix, value: out Dictionary<string, Console.Command>? module))
		{
			response = $"invalid prefix: {input.Prefix}";
			return;
		}
		if (!module.TryGetValue(input.Phrase, value: out Console.Command? command))
		{
			response = $"invalid phrase : {input.Phrase}";
			return;
		}
		StringBuilder? responseBuilder = null;

		if (command is null) { return; }
		if (input.IsEmpty())
		{
			command.Default();
			response = "input empty... executing default command.";
			return;
		}

		foreach (string flag in input.Flags)
		{
			if (!command.Flags.TryGetValue(flag, out Action? flagAction))
			{
				(responseBuilder ??= new StringBuilder()).AppendLine($"({flag}) is not a registered flag");
				continue;
			}
			flagAction();
		}
		foreach ((string key, object obj) in input.Properties)
		{
			if (!command.Properties.TryGetValue(key, out Action<object>? value))
			{
				(responseBuilder ??= new StringBuilder()).AppendLine($"({key}) is not a registered property");
				continue;
			}
			value(obj);
		}
		response = responseBuilder?.ToString();

		//IEnumerable<string> flags = input.Flags.Where(command.Flags.ContainsKey);
		//IEnumerable<KeyValuePair<string, object>> props = input.Properties
		//	.Where(k => command.Properties.ContainsKey(k.Key));
		//foreach (string key in flags)
		//{
		//	if (!command.Flags.TryGetValue(key, out Action? value))
		//	{
		//		responseBuilder.AppendLine($"({key}) is not a registered flag");
		//		continue;
		//	}
		//	value();
		//}
		//foreach ((string key, object obj) in props)
		//{
		//	if (!command.Properties.TryGetValue(key, out Action<object>? value))
		//	{
		//		responseBuilder.AppendLine($"({key}) is not a registered property");
		//		continue;
		//	}
		//	value(obj);
		//}
		//response = responseBuilder.Length != 0 ? responseBuilder.ToString() : null;
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
	//public static void ParseToken()
}

