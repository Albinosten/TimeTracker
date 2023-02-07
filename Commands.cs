using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace TimeTrackerApp
{
	public class CommandRequest : ICommandRequest
	{
		public string Arg { get; set; }
		public bool ExactSearch { get; set; }
	}
	public enum CommandNumbers
	{
		Invalid = -1,
		Start = 1,
		Stop = 2,
		List = 3,
		Active = 4,
		GetTime = 5,
		Day = 6,
		Restart = 7,
		Commands = 9,
		Restore = 89,
		Backup = 90,
		Week = 91,
		Merge = 92,
		NewHistoric = 95,
		Rename = 96,
		Delete = 97,
		Reset = 98,
		Exit = 99,
	}
	public class Start : ICommandWithArg
	{
		public IList<string> Aliases => EmptyList.List;
		public int CommandNumber => (int)CommandNumbers.Start;

		public bool Execute(ICommandRequest request)
		{
			var fileHandler = new FileHandler();

			var log = new Log
			{
				StartTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
				Action = Action.Start,
				Name = request.Arg,
			};
			fileHandler.Create(log);
			Console.Clear();

			var list = new List();
			list.Execute(request);

			return true;
		}
		public bool Execute()
		{
			PrintWithColor.WriteLine("Start name: ");
			var request = new CommandRequest()
			{
				Arg = InteruptableReader.ReadLine(),
				ExactSearch = true,
			};
			if (string.IsNullOrEmpty(request.Arg))
			{
				PrintWithColor.WriteLine("Must set a name");
				return true;
			};
			this.Execute(request);

			return true;
		}
	}
	public class Stop : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Stop;
		private int number => 100; 
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Stop: ");
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.Where(x => x.Action == Action.Start)
				.TakeLast(number)
				.GroupBy(x => x.Name)
				.ToList();
			var logsToRemove = new List<Log>();

			PrintWithColor.WriteLine("Enter for all", ConsoleColor.DarkRed);
			for (int i = 0; i < Math.Min(number, logs.Count); i++)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key);
			}
			PrintWithColor.WriteLine("x for cancel", ConsoleColor.DarkRed);

			var input = InteruptableReader.ReadLine();
			if (int.TryParse(input, out int index)
				&& index < Math.Min(number, logs.Count)
				&& index >= 0
				)
			{
				logsToRemove.AddRange(logs[index]);
			}
			else if (string.IsNullOrEmpty(input))
			{
				logsToRemove.AddRange(logs.SelectMany(x => x));
			}

			foreach (var log in logsToRemove)
			{
				fileHandler.Delete(log);
				log.StopTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
				log.Action = Action.Stop;
				fileHandler.Create(log);
			}

			return true;
		}
	}
	public class List : ICommandWithArg
	{
		public virtual int CommandNumber => (int)CommandNumbers.List;
		public virtual IList<string> Aliases => new[] { "Ls" };
		protected IEnumerable<Log> GetLogs(ICommandRequest request)
		{
			var fileHandler = new FileHandler();
			return fileHandler.GetAllLogs(request);
		}
		protected void Print(IList<Log> logs)
		{
			foreach (var log in logs)
			{
				PrintWithColor.Write(log.DisplayReadable());
				PrintWithColor.WriteLine(" time: " + log.GetTimeSpan()
					, background: log.Action == Action.Start ? ConsoleColor.Blue : default);
			}
			PrintWithColor.WriteLine("Count: " + logs.Count);
		}
		public bool Execute(ICommandRequest request)
		{
			this.Print(this.GetLogs(request).ToList());
			return true;
		}
		public virtual bool Execute()
		{
			PrintWithColor.WriteLine("Name: ");
			var request = new CommandRequest()
			{
				Arg = InteruptableReader.ReadLine(),
			};
			this.Execute(request);
			return true;
		}
	}
	public class Active : List
	{
		public override int CommandNumber => (int)CommandNumbers.Active;
		public int TimeoutTime => TimeTracker.NormalTimeoutTime;
		public override IList<string> Aliases => EmptyList.List; 
		public override bool Execute()
		{
			var lines = this.GetLogs(new CommandRequest())
				.Where(x => x.Action == Action.Start)
				.ToList();

			this.Print(lines);
			while (lines.Any() && !InteruptableReader.TryReadLine(out string _, this.TimeoutTime))
			{
				Console.Clear();
				this.Print(lines);
			}

			return true;
		}
	}
	public class Week : Day
	{
		protected override string Prompt => "Number of weeks: ";
		public override int CommandNumber => (int)CommandNumbers.Week;

		protected override string GroupBy(Log log)
		{
			DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
			Calendar cal = dfi.Calendar;

			return $"Week: {cal.GetWeekOfYear(DateTimeOffset.FromUnixTimeSeconds(log.StartTimestamp).Date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek)}";
		}
	}
	public class Day : ICommand
	{
		public virtual int CommandNumber => (int)CommandNumbers.Day;

		private static int StartNumber => 7;
		protected virtual string GroupBy(Log log)
		{
			return $"{DateTimeOffset.FromUnixTimeSeconds(log.StartTimestamp).Date:MMM-dd}";
		}

		protected virtual string Prompt => "Number of days: ";

		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			PrintWithColor.WriteLine(this.Prompt);
			_ = int.TryParse(InteruptableReader.ReadLine(), out int number);
			number = number == 0 ? StartNumber : number;

			var groupedByDay = new FileHandler()
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => this.GroupBy(x))
				.TakeLast(number)
				;
			foreach (var day in groupedByDay)
			{
				var groupedByName = day
					.GroupBy(x => x.Name);
				foreach (var name in groupedByName)
				{
					var totalTimePerName = TimeSpan.FromMinutes(name
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes));

					PrintWithColor.WriteLine(name.First().Name + " time: " + totalTimePerName
						, background: name.Any(x => x.Action == Action.Start) ? ConsoleColor.Blue : default);
				}
				var totalTime = TimeSpan.FromMinutes(day
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes));

				PrintWithColor.WriteLine(day.Key + " Total time: " + totalTime
					, background: day.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
				PrintWithColor.WriteLine("");
			}
			return true;
		}
	}

	public class GetTime : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.GetTime;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Get time: ");

			var fileHandler = new FileHandler();

			var request = new CommandRequest()
			{
				Arg = InteruptableReader.ReadLine(),
			};

			var groupPerNames = fileHandler
				.GetAllLogs(request)
				.GroupBy(x => x.Name)
				.OrderByDescending(x => x.Min(m => m.Action))
				.ThenBy(x => x.Max(x => x.StopTimestamp))
				.ToList()
				;
			foreach (var groupPerName in groupPerNames)
			{
				var groupPerDay = groupPerName
					.GroupBy(x => DateTimeOffset.FromUnixTimeSeconds(x.StartTimestamp).Date)
					.ToList();

				foreach (var group in groupPerDay)
				{
					foreach (var log in group)
					{
						PrintWithColor.WriteLine(log.DisplayReadable(x => x.Name, x => x.StartTime, x => x.StopTime) + " time: " + log.GetTimeSpan()
							, background: log.Action == Action.Start ? ConsoleColor.Blue : default);
					}

					if (groupPerDay.Count > 1)
					{
						var totalTimeForDay = TimeSpan.FromMinutes(group
							.Select(x => x.GetTimeSpan())
							.Sum(x => x.TotalMinutes));

						PrintWithColor.WriteLine($"{group.Key:MMM-dd}" + " Sum: " + totalTimeForDay
							, background: group.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
					}
				}

				var date = groupPerDay.Count > 1 ? "" : $"{groupPerDay[0].Key:MMM-dd} ";
				var totalTime = TimeSpan.FromMinutes(groupPerName
					.Select(x => x.GetTimeSpan())
					.Sum(x => x.TotalMinutes));
				PrintWithColor.WriteLine(date + "Sum: " + totalTime
					, background: groupPerName.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);

				PrintWithColor.WriteLine("");
			}
			return true;
		}
	}

	public class Rename : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Rename;
		private int number => 100;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			PrintWithColor.WriteLine("Rename entry:");
			var logs = fileHandler
				.GetAllLogs(new CommandRequest() { Arg = InteruptableReader.ReadLine() })
				.GroupBy(x => x.Name)
				.TakeLast(number)
				.ToList();

			for (int i = 0; i < Math.Min(number, logs.Count); i++)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key
					, background: logs[i].Any(x => x.Action == Action.Start) ? ConsoleColor.Blue : default);
			}
			PrintWithColor.WriteLine("Any key to return.", ConsoleColor.DarkRed);
			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(number, logs.Count)
				&& index >= 0
				)
			{
				PrintWithColor.WriteLine("New name:");
				var newName = InteruptableReader.ReadLine();
				if (string.IsNullOrEmpty(newName))
				{
					PrintWithColor.WriteLine("Must set a name");
					return true;
				};
				var logsToRename = logs[index].ToList();
				fileHandler.Delete(logsToRename);
				foreach (var log in logsToRename)
				{
					log.Name = newName;
				}
				fileHandler.Create(logsToRename);
			}
			return true;
		}
	}
	public class Delete : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Delete;
		private int number => 100;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			PrintWithColor.WriteLine("Remove entry with name:");
			var logs = fileHandler
				.GetAllLogs(new CommandRequest() { Arg = InteruptableReader.ReadLine() })
				.TakeLast(number)
				.ToList();

			for (int i = 0; i < Math.Min(number, logs.Count); i++)
			{
				PrintWithColor.Write(i + " : " + logs[i].DisplayReadable());
				PrintWithColor.WriteLine(" time: " + logs[i].GetTimeSpan(), background: logs[i].Action == Action.Start ? ConsoleColor.Blue : default);
			}
			PrintWithColor.WriteLine("Any key to return.", ConsoleColor.DarkRed);
			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(number, logs.Count)
				&& index >= 0
				)
			{
				fileHandler.Delete(logs[index]);
			}
			return true;
		}
	}
	public class Reset : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Reset;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{

			PrintWithColor.WriteLine("Sure to delete all? type 'yes' to accept");
			if (Console.ReadLine() == "yes")
			{

				var fileHandler = new FileHandler();
				fileHandler.Reset();
			}
			return true;
		}
	}
	public class Backup : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Backup;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var newName = fileHandler.Backup();
			PrintWithColor.WriteLine("Backup " + newName);
			return true;
		}
	}
	public class Restore : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Restore;

		public IList<string> Aliases => EmptyList.List;
		private static int number => 25;

		public bool Execute()
		{
			var filehandler = new FileHandler();

			var files = filehandler
				.GetAllFiles()
				.Select(x => x.Remove(0, FileHandler.Location.Length))
				.TakeLast(number)
				.ToList()
				;

			PrintWithColor.WriteLine("Restore from:");
			int maxWidth = files.Max(x => x.Length);


			var header = "Filename" + new string(' ', (maxWidth - 8) + 1);
			PrintWithColor.WriteLine($"{"Nr",2} | " + String.Format($"{header}| {"Size",4} KB"));
			for (int i  = 0; i < files.Count; i++)
			{
				var fileName = files[i] + new string(' ', (maxWidth - files[i].Length) + 1);
				PrintWithColor.WriteLine($"{i,2} | " + String.Format($"{fileName}|{filehandler.GetFileSize(files[i])/1000,4} KB"));
			}
			PrintWithColor.WriteLine("X for cancel", ConsoleColor.DarkRed);

			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(files.Count, number)
				&& index >= 0
				)
			{
				filehandler.Restore(files[index]);
			}

			return true;
		}
	}
	public class Restart : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Restart;
		static int number => 25; 
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => x.Name)
				.TakeLast(number)
				.OrderByDescending(x => x.Max(mbox => mbox.StartTimestamp))
				.ToList();

			for (int i = 0; i < Math.Min(logs.Count, number); i++)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key);
			}
			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(logs.Count, number)
				&& index >= 0
				)
			{
				var request = new CommandRequest()
				{
					Arg = logs[index].Key,
					ExactSearch = true,
				};
				var start = new Start();
				start.Execute(request);
			}
			return true;
		}
	}
	public class AddNewHistoric : ICommand
	{
		public IList<string> Aliases => new []{ "Add", "New", "Historic"};
		public int CommandNumber => (int)CommandNumbers.NewHistoric;

		public bool Execute()
		{
			PrintWithColor.WriteLine("Start name: ");
			var name = InteruptableReader.ReadLine();
			if (string.IsNullOrEmpty(name))
			{
				PrintWithColor.WriteLine("Must set a name");
				return true;
			};

			PrintWithColor.WriteLine("Minutes: ");
			var input = InteruptableReader.ReadLine();
			if (!int.TryParse(input, out var minutes))
			{
				PrintWithColor.WriteLine("Invalid");
				return true;
			};


			var time = (DateTimeOffset)DateTime.Today.AddHours(3);
			var log = new Log
			{
				StartTimestamp = time.ToUnixTimeSeconds(),
				StopTimestamp = time.ToUnixTimeSeconds() + minutes * 60,
				Action = Action.Stop,
				Name = name,
			};

			var fileHandler = new FileHandler();
			fileHandler.Create(log);
			Console.Clear();

			var list = new List();
			list.Execute(new CommandRequest { Arg = name, ExactSearch = true });

			return true;
		}
	}
	public class Merge : ICommand
	{
		private static int number => 25;
		public int CommandNumber => (int)CommandNumbers.Merge;
		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => x.Name)
				.OrderByDescending(x => x.Max(mbox => mbox.StartTimestamp))
				.TakeLast(number)
				.ToList();

			PrintWithColor.WriteLine("Merge names");

			for (int i = 0; i < Math.Min(logs.Count, number); i++)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key);
			}

			if (int.TryParse(InteruptableReader.ReadLine(), out int targetIndex)
				&& targetIndex < Math.Min(logs.Count, number)
				&& targetIndex >= 0
				)
			{
				Console.Clear();
				
				PrintWithColor.WriteLine(targetIndex.ToString());
				PrintWithColor.WriteLine("New name: ");
				for (int i = 0; i < Math.Min(logs.Count, number); i++)
				{
					if (i != targetIndex)
					{
						PrintWithColor.WriteLine(i + " : " + logs[i].Key);
					}
				}

				if (int.TryParse(InteruptableReader.ReadLine(), out int sourceIndex)
					&& sourceIndex < Math.Min(logs.Count, number)
					&& sourceIndex >= 0
				)
				{
					fileHandler.Delete(logs[targetIndex]);
					foreach (var log in logs[targetIndex])
					{
						log.Name = logs[sourceIndex].Key;
					}
					fileHandler.Create(logs[targetIndex]);
				}
			}
			return true;
		}
	}
	
	public class Commands : ICommand
	{
		public IList<string> Aliases => new[] 
		{ 
			"C" ,
			"Cmd", 
		};
		public int CommandNumber => (int)CommandNumbers.Commands;

		public bool Execute()
		{
			var commands = CommandCreator.CreateAll();
			foreach (var command in commands)
			{
				PrintWithColor.WriteLine(command.CommandNumber + " " + command.GetType().Name);
				for(int i = 0; i < command.Aliases.Count; i++)
				{
					var line = i == command.Aliases.Count - 1
						? "└── "
						: "├── ";
					PrintWithColor.WriteLine(line + command.Aliases[i]);
				}
			}
			return true;
		}
	}
	public class Invalid : ICommand
	{
		public IList<string> Aliases => EmptyList.List;

		public int CommandNumber => (int)CommandNumbers.Invalid;

		public bool Execute()
		{
			PrintWithColor.WriteLine("Invalid operation");
			return true;
		}
	}
	public class Meny : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Invalid;

		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			var commands = new[]
			{
				typeof(Start),
				typeof(Stop),
				typeof(List),
				typeof(Active),
				typeof(GetTime),
				typeof(Day),
				typeof(Restart),
				typeof(Commands),
			};

			foreach(var command in commands.Select(CommandCreator.Create))
			{
				PrintWithColor.WriteLine(command.CommandNumber + " : " + command.GetType().Name);
			}
			PrintWithColor.WriteLine("X"+ " : " + typeof(Exit).Name);

			return true;
		}
	}

	public class Exit : ICommand
	{
		public int CommandNumber => (int)CommandNumbers.Exit;

		public IList<string> Aliases => new[] {"X", "Q"};

		public bool Execute()
		{
			return false;
		}
	}

	public static class EmptyList
	{
		public static IList<string> List => Array.Empty<string>().ToList();
	}
}