using System;
using System.Collections.Generic;
using System.Linq;
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
		public static string ToTotalHours(this TimeSpan timespan)
		{
			return $"{((int)timespan.TotalHours).AddPadding()}:{timespan.Minutes.AddPadding()}:{timespan.Seconds.AddPadding()}";
		}
		public static string AddPadding(this int value)
		{
			return value.ToString().PadLeft(2, '0');
		}
	}

	public interface ICommandRequest 
	{
		string Arg { get; set; }
		bool ExactSearch { get; set; }
		//bool Clear { get; set; }
	}
	public interface ICommandWithArg : ICommand
	{
		bool Execute(ICommandRequest request);
	}

	public interface ICommand
	{
		bool Execute();
		CommandNumbers CommandNumber { get; }
		IList<string> Aliases { get; }
	}

	//TODO: ta in args för location path.
	//TODO: alla commands kan ta in FileHandler som ett dependency. 
			//Då behöver man inte definera path i koden och kan byta den utan att behöva kompilera.
	//TODO användarnamn som args? då kan man ha flera användare för samma logfil.
	//TODO Taggar på loggar. för att kunna få tid per tagg
	public class TimeTracker
	{
		public static int NormalTimeoutTime => 6 * 1000;
		static void Main(string[] args)
		{
			Console.Clear();
			while (CommandCreator.Create(typeof(Meny)).Execute())
			{
			}
		}
	}
	public static class CommandCreator
	{
		public  static ICommand GetCommand(string c)
		{
			var types = CreateAll();
			foreach (var command in types)
			{
				if (c.ToUpper() == command.GetType().Name.ToUpper())
				{
					return command;
				}
				else if (c.ToUpper() == ((int)command.CommandNumber).ToString())
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

		public static ICommandWithArg Create<T>()
			where T : ICommandWithArg
		{
			return (ICommandWithArg)Create(typeof(T));
		}
		public static ICommand Create(Type type)
		{
			return (ICommand)type.GetConstructor(Type.EmptyTypes).Invoke(null);
		}

		public static IList<ICommand> CreateAll()
		{
			return AppDomain.CurrentDomain.GetAssemblies()
				.SelectMany(s => s.GetTypes())
				.Where(p => typeof(ICommand).IsAssignableFrom(p))
				.Where(p => p.IsClass)
				.Select(type => Create(type))
				.Where(x => (int)x.CommandNumber > 0)
				.OrderBy(x => (int)x.CommandNumber)
				.ToList();
		}
	}
}
