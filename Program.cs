using System;

namespace TimeTrackerApp
{
	public static class Extension
	{
		public static string Sanitize(this string input)
		{
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
		int TimeoutTime { get; }
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
			int timeoutTime = NormalTimeoutTime;
			while (run) 
			{
				try
				{
					PrintWithColor.WriteLine("1 : Create New");
					PrintWithColor.WriteLine("2 : Stop");
					PrintWithColor.WriteLine("3 : List");
					PrintWithColor.WriteLine("4 : List Active");
					PrintWithColor.WriteLine("5 : Get Time");
					PrintWithColor.WriteLine("6 : Get Time Per Day");
					PrintWithColor.WriteLine("7 : Restart Recent");
					//Console.WriteLine("Reset file RESET:");
					PrintWithColor.WriteLine("X : Exit");
					var key = InteruptableReader.ReadLine(timeoutTime);
					var command = GetCommand(GetAlias(key));
					timeoutTime = command.TimeoutTime;
					Console.Clear();
					run = command.Execute();
					PrintWithColor.WriteLine("*************************************");
				}
				catch (TimeoutException)
				{
					var command = GetCommand(GetAlias("ACTIVE"));
					Console.Clear();
					run = command.Execute();
					PrintWithColor.WriteLine("*************************************");
				}
			}
		}
		private static ICommand GetCommand(int a) => a switch
		{
			1 => new Start(),
			2 => new Stop(),
			3 => new List(),
			4 => new ListActive(),
			5 => new GetTime(),
			6 => new GetTimePerDay(),
			7 => new RestartRecent(),
			92 => new Merge(),
			//93 => new remove time(),
			//94 => new add time(),
			95 => new CreateNewHistoric(),
			96 => new Rename(),
			97 => new Delete(),
			98 => new Reset(),
			99 => new Exit(),
			_ => new Invalid(),
		};
		private static int GetAlias(string c) => c.ToUpper() switch
		{
			"1" => 1,
			"2" => 2,
			"3" => 3,
			"4" => 4,
			"5" => 5,
			"6" => 6,
			"7" => 7,
			"START" => 1,
			"S" => 1,
			"Q" => 2,
			"QUIT" => 2,
			"STOP" => 2,
			"END" => 2,
			"LIST" => 3,
			"L" => 3,
			"LS" => 3,
			"ACTIVE" => 4,
			"G" => 5,
			"GET" => 5,
			"TIME" => 5,
			"MERGE" => 92,
			"REMOVE" => 93,
			"ADD" => 94,
			"NEW" => 95,
			"RENAME" => 96,
			"DELETE" => 97,
			"DELETE ALL" => 98,
			"X" => 99,
			_ => -1,
		};
	}
}
