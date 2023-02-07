using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace TimeTrackerApp
{
	public static class Extension
	{
		public static string Sanitize(this string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return "";
			}
			return input
				.Replace(',', ' ')
				.Trim()
				;
		}
	}
	public interface ICommandRequest 
	{
		string Arg { get; set; }
		bool ExactSearch { get; set; }
	}
	public interface ICommandWithArg : ICommand
	{
		bool Execute(ICommandRequest request);
	}
	public interface ICommand
	{
		bool Execute();
		int CommandNumber { get; }
		IList<string> Aliases { get; }
	}
	public class TimeTracker
	{
		public static int NormalTimeoutTime => 6 * 1000;
		static void Main(string[] args)
		{
#if DEBUG

#endif
			Console.Clear();
			bool run = true;
			while (run)
			{
				CommandCreator.Create(typeof(Meny)).Execute();
				var command = GetCommand(InteruptableReader.ReadLine());
				Console.Clear();
				run = command.Execute();
				PrintWithColor.WriteLine("*************************************");
			}
		}

		private static ICommand GetCommand(string c)
		{
			var types = CommandCreator.CreateAll();
			foreach (var command in types)
			{
				if (c.ToUpper() == command.GetType().Name.ToUpper())
				{
					return command;
				}
				else if (c.ToUpper() == command.CommandNumber.ToString())
				{
					return command;
				}
				else if(command.Aliases.Select(x => x.ToUpper()).Contains(c.ToUpper()))
				{
					return command;
				}
			}
			return new Invalid();
		}
	}

	public static class CommandCreator
	{
		public static ICommand Create(Type type)
		{
			return (ICommand)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		public static IList<ICommand> CreateAll()
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(x => x != typeof(Meny))
				.Where(x => x != typeof(Invalid))
				.Where(p => typeof(ICommand).IsAssignableFrom(p))
				.Where(p => p.IsClass)
				.Select(type => Create(type))
				.OrderBy(x => x.CommandNumber)
				.ToList();
		}
	}
}
