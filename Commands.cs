using System;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace TimeTrackerApp
{
	public class Start : ICommandWithArg
	{
		public IList<string> Aliases => EmptyList.List;
		public CommandNumbers CommandNumber => CommandNumbers.Start;

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

			var list = CommandCreator.Create<Active>();
			list.Execute();

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
		public CommandNumbers CommandNumber => CommandNumbers.Stop;
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
		public virtual CommandNumbers CommandNumber => CommandNumbers.List;
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
				PrintWithColorHelper.PrintLog(log);
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
		public override CommandNumbers CommandNumber => CommandNumbers.Active;
		public int TimeoutTime => TimeTracker.NormalTimeoutTime;
		public override IList<string> Aliases => EmptyList.List; 
		public override bool Execute()
		{
			var lines = this.GetLogs(new CommandRequest())
				.Where(x => x.Action == Action.Start)
				.ToList();
			this.Print(lines);

			if (lines.Any()) 
			{
				var menyPrinter = CommandCreator.Create(typeof(MenyPrinter));
				menyPrinter.Execute();
				string input = "";
				while (lines.Any() && !InteruptableReader.TryReadLine(out input, this.TimeoutTime))
				{
					Console.Clear();
					this.Print(lines);
					menyPrinter.Execute();
				}
				CommandCreator
					.Create<Meny>()
					.Execute(new CommandRequest
					{ 
						Arg = input,
					});
			}
			return true;
		}
	}
	public class Week : Day
	{
		protected override string Prompt => "Number of weeks: ";
		public override CommandNumbers CommandNumber => CommandNumbers.Week;

		protected override string GroupBy(Log log)
		{
			DateTimeFormatInfo dfi = DateTimeFormatInfo.CurrentInfo;
			Calendar cal = dfi.Calendar;
			var timestamp = DateTimeOffset.FromUnixTimeSeconds(log.StartTimestamp);
			return $"{timestamp.Year} Week: {cal.GetWeekOfYear(timestamp.Date, dfi.CalendarWeekRule, dfi.FirstDayOfWeek)}";
		}
	}
	public class Day : ICommand
	{
		public virtual CommandNumbers CommandNumber => CommandNumbers.Day;

		private static int StartNumber => int.MaxValue;
		protected virtual string GroupBy(Log log)
		{
			var timestamp = DateTimeOffset.FromUnixTimeSeconds(log.StartTimestamp);
			if (timestamp.Year > 2000)
			{
				return $"{timestamp.Date:yy-MMM-dd}";
			}
			return $"{timestamp.Date:yyyy-MMM-dd}";
		}

		protected virtual string Prompt => "Number of days: ";

		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			int number = 0;
			var groupedByDay = new FileHandler()
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => this.GroupBy(x))
				.OrderBy(x => x.Max(x => x.StartTimestamp))
				.TakeLast(number == 0 ? StartNumber : number)
				;
			foreach (var day in groupedByDay)
			{
				var groupedByName = day
					.GroupBy(x => x.Name);
				foreach (var name in groupedByName)
				{
					var totalTimePerName = TimeSpan.FromMinutes(name
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes))
						.ToTotalHours();

					PrintWithColor.WriteLine(name.First().Name + " time: " + totalTimePerName
						, background: name.Any(x => x.Action == Action.Start) ? ConsoleColor.Blue : null);
				}
				var totalTime = TimeSpan.FromMinutes(day
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes))
						.ToTotalHours();

				PrintWithColor.WriteLine(day.Key + " Total time: " + totalTime
					, background: day.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
				PrintWithColor.WriteLine("");
			}
			return true;
		}
	}

	public class GetTime : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.GetTime;
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
					.OrderBy(x => x.Key)
					.ToList();

				foreach (var group in groupPerDay)
				{
					foreach (var log in group)
					{
						PrintWithColor.WriteLine(log.DisplayReadable(x => x.Name, x => x.StartTime, x => x.StopTime) + " time: " + log.GetTimeSpan()
							, background: log.Action == Action.Start ? ConsoleColor.Blue : null);
					}

					if (groupPerDay.Count > 1)
					{
						var totalTimeForDay = TimeSpan.FromMinutes(group
							.Select(x => x.GetTimeSpan())
							.Sum(x => x.TotalMinutes))
							.ToTotalHours();

						PrintWithColor.WriteLine($"{group.Key:MMM-dd}" + " Sum: " + totalTimeForDay
							, background: group.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
					}
				}

				var date = groupPerDay.Count > 1 ? "" : $"{groupPerDay[0].Key:MMM-dd} ";
				var totalTime = TimeSpan.FromMinutes(groupPerName
					.Select(x => x.GetTimeSpan())
					.Sum(x => x.TotalMinutes))
					.ToTotalHours();
				PrintWithColor.WriteLine(date + "Sum: " + totalTime
					, background: groupPerName.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);

				PrintWithColor.WriteLine("");
			}
			return true;
		}
	}

	public class Rename : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.Rename;
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

			for (int i = Math.Min(number, logs.Count) - 1; i >= 0; i--)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key
					, background: logs[i].Any(x => x.Action == Action.Start) ? ConsoleColor.Blue : null);
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
		public CommandNumbers CommandNumber => CommandNumbers.Delete;
		private int number => 100;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			PrintWithColor.WriteLine("Remove entry with name:");
			var logs = fileHandler
				.GetAllLogs(new CommandRequest() { Arg = InteruptableReader.ReadLine() })
				.TakeLast(number)
				.Reverse()
				.ToList();

			for (int i = Math.Min(number, logs.Count) -1; i >= 0; i--)
			{
				PrintWithColorHelper.PrintLog(logs[i], i + " : ");
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
		public CommandNumbers CommandNumber => CommandNumbers.Reset;
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
		public CommandNumbers CommandNumber => CommandNumbers.Backup;
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
		public CommandNumbers CommandNumber => CommandNumbers.Restore;

		public IList<string> Aliases => EmptyList.List;
		private static int number => 20;

		public bool Execute()
		{
			var filehandler = new FileHandler();

			var allFiles = filehandler
				.GetAllFiles();
			var files= allFiles
				.Item1
				.OrderByDescending(x => x)
				.Take(number)
				.ToList()
				;

			PrintWithColor.WriteLine("Restore from:");
			int maxWidth = files.Max(x => x.Item1.Length);
			var width = maxWidth + 2;

			var header = "Filename" + new string(' ', (maxWidth - 8) + 1);
			PrintWithColor.WriteLine($"{"Nr",2} │ " + String.Format($"{header}│ {"Size",4} KB"));
			PrintWithColor.WriteLine("───┼" + new string('─', width ) + "┼" + new string('─', 7));
			for (int i  = 0; i < files.Count; i++)
			{
				var fileName = files[i].Item1 + new string(' ', (maxWidth - files[i].Item1.Length) + 1);
				PrintWithColor.WriteLine($"{i,2} │ " + String.Format($"{fileName}│{files[i].Item2/1000,4} KB"));
			}

			var totalSize = (allFiles.Item2/1000).ToString();
			var size = files.Sum(x => x.Item2 / 1000).ToString();

			PrintWithColor.WriteLine("───┴" + new string('─', width) + "┴"+ new string('─', 7));
			PrintWithColor.WriteLine("Size:" + new string(' ', width - size.Length + 5) + size + "KB");
			PrintWithColor.WriteLine("Total size:" + new string(' ', width - totalSize.Length - 1) + totalSize + "KB");

			PrintWithColor.WriteLine("");
			PrintWithColor.WriteLine("X for cancel", ConsoleColor.DarkRed);

			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(files.Count, number)
				&& index >= 0
				)
			{
				filehandler.Restore(files[index].Item1);
			}

			return true;
		}
	}
	public class Restart : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.Restart;
		static int number => int.MaxValue;
		public IList<string> Aliases => EmptyList.List;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => x.Name)
				.OrderByDescending(x => x.Max(mbox => mbox.StartTimestamp))
				.Take(number)
				.ToList();

			for (int i = Math.Min(logs.Count, number) - 1; i >= 0; i--)
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
				var start = CommandCreator.Create<Start>();
				start.Execute(request);
			}
			return true;
		}
	}
	public class AddNewHistoric : ICommand
	{
		public IList<string> Aliases => new []{ "Add", "New", "Historic"};
		public CommandNumbers CommandNumber => CommandNumbers.NewHistoric;

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

			var list = CommandCreator.Create<List>();
			list.Execute(new CommandRequest { Arg = name, ExactSearch = true });

			return true;
		}
	}
	public class MergeNames : ICommand
	{
		private static int number => int.MaxValue;
		public CommandNumbers CommandNumber => CommandNumbers.Merge;
		public IList<string> Aliases => new[] { "Merge" };

		public bool Execute()
		{
			PrintWithColor.WriteLine("Name");
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest { Arg = InteruptableReader.ReadLine()})
				.GroupBy(x => x.Name)
				.OrderByDescending(x => x.Max(mbox => mbox.StartTimestamp))
				.Take(number)
				.ToList();

			PrintWithColor.WriteLine("Merge names");

			for (int i = Math.Min(logs.Count, number) - 1; i >= 0 ; i--)
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
				for (int i = Math.Min(logs.Count, number) - 1; i >= 0; i--)
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
		public CommandNumbers CommandNumber => CommandNumbers.Commands;

		public bool Execute()
		{
			var commands = CommandCreator.CreateAll();
			foreach (var command in commands)
			{
				var helptext = command.CommandNumber.GetHelpText();
				PrintWithColor.WriteLine((int)command.CommandNumber + " " + command.GetType().Name + (!string.IsNullOrEmpty(helptext) ? ": " + helptext : string.Empty));
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

		public CommandNumbers CommandNumber => CommandNumbers.Invalid;

		public bool Execute()
		{
			PrintWithColor.WriteLine("Invalid operation");
			return true;
		}
	}
	public class MenyPrinter : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.Invalid;

		public IList<string> Aliases => throw new NotImplementedException();

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
				typeof(Week),
				typeof(Commands),
			};

			PrintWithColor.WriteLine("*************************************");
			foreach (var command in commands.Select(CommandCreator.Create))
			{
				PrintWithColor.WriteLine((int)command.CommandNumber + " : " + command.GetType().Name);
			}
			PrintWithColor.WriteLine("X" + " : " + typeof(Exit).Name);
			return true;
		}
	}
	public class Meny : ICommandWithArg
	{
		public CommandNumbers CommandNumber => CommandNumbers.Invalid;

		public IList<string> Aliases => EmptyList.List;
		
		public bool Execute()
		{	
			CommandCreator.Create(typeof(MenyPrinter)).Execute();
			return this.Execute(new CommandRequest { Arg = InteruptableReader.ReadLine() });
		}

		public bool Execute(ICommandRequest request)
		{
			var nextCommand = CommandCreator.GetCommand(request.Arg);
			Console.Clear();

			return nextCommand.Execute();
		}
	}

	public class EditEndTime : EditStartTime
	{
		public override CommandNumbers CommandNumber => CommandNumbers.EditEndTime;

		public override IList<string> Aliases => new[] { "EditEnd", "EEnd", };
		protected override  void EditTime(Log log, int value)
		{
			log.StopTimestamp += value;
		}
		protected override bool Filter(Log log)
		{
			return log.StopTimestamp != decimal.Zero;
		}
	}
	public class EditStartTime : ICommand
	{
		public virtual CommandNumbers CommandNumber => CommandNumbers.EditStartTime;

		public virtual IList<string> Aliases => new[] { "EditStart", "EStart", "Edit", };
		private int number => 100;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Name:");

			var request = new CommandRequest { Arg = InteruptableReader.ReadLine() };

			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(request)
				.Where(Filter)
				.TakeLast(number)
				.Reverse()
				.ToList()
				;
			for (int i = Math.Min(logs.Count, number) - 1; i >= 0; i--)
			{
				PrintWithColorHelper.PrintLog(logs[i], i + " : ");
			}

			PrintWithColor.WriteLine("");
			PrintWithColor.WriteLine("X for cancel", ConsoleColor.DarkRed);

			if (int.TryParse(InteruptableReader.ReadLine(), out int index)
				&& index < Math.Min(logs.Count, number)
				&& index >= 0
				)
			{
				PrintWithColor.WriteLine("Minutes:");
				if(int.TryParse( InteruptableReader.ReadLine(), out int value))
				{
					var log = logs[index];
					fileHandler.Delete(log);
					this.EditTime(log, (value * 60));
					fileHandler.Create(log);
				}
			}

			return true;
		}
		protected virtual bool Filter(Log log)
		{
			return true;
		}
		protected virtual void EditTime(Log log, int value)
		{
			log.StartTimestamp -= value;
		}
	}

	public class Exit : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.Exit;

		public IList<string> Aliases => new[] {"X", "Q"};

		public bool Execute()
		{
			return false;
		}
	}
	public class SquishWeek : ICommand
	{
		//Squish all entries on this week into one per name.
		public CommandNumbers CommandNumber => CommandNumbers.Invalid;

		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			PrintWithColor.WriteLine(this.GetType().Name);
			return true;
		}
	}
	public class SquishDay : ICommand
	{
		public CommandNumbers CommandNumber => CommandNumbers.Invalid;

		public IList<string> Aliases => EmptyList.List;

		public bool Execute()
		{
			PrintWithColor.WriteLine(this.GetType().Name);

			return true;
		}
	}
	public static class EmptyList
	{
		public static IList<string> List => Array.Empty<string>().ToList();
	}
}