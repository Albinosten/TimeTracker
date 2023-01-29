using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;

namespace TimeTrackerApp
{
	public class CommandRequest : ICommandRequest
	{
		public string Arg { get; set; }
		public bool ExactSearch { get; set; }
	}
	public class Start : ICommandWithArg
	{
		public int TimeoutTime => TimeTracker.NormalTimeoutTime;
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
		public int TimeoutTime => TimeTracker.NormalTimeoutTime;

		private int number => 99;
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
		public virtual int TimeoutTime => Timeout.Infinite;
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
					, background: log.Action == Action.Start ? ConsoleColor.Blue: default);
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
	public class ListActive : List
	{
		public override int TimeoutTime => TimeTracker.NormalTimeoutTime;
		public override bool Execute()
	   {
			var lines = this.GetLogs(new CommandRequest())
				.Where(x => x.Action == Action.Start)
				.ToList();
			this.Print(lines);
			return true;
	   }
	}

	public class GetTimePerDay : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		private static int StartNumber => 7;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Number of days: ");
			_ = int.TryParse(InteruptableReader.ReadLine(), out int number);
			number = number == 0 ? StartNumber: number;

			var groupedByDay = new FileHandler()
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => DateTimeOffset.FromUnixTimeSeconds(x.StartTimestamp).Date)
				.TakeLast(number)
				;
			foreach(var day in groupedByDay)
			{
				var groupedByName = day
					.GroupBy(x => x.Name);
				foreach(var name in groupedByName)
				{
					var totalTimePerName = TimeSpan.FromMinutes(name
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes));

					PrintWithColor.WriteLine(name.First().Name + " time: " + totalTimePerName
						, background: name.Any(x => x.Action == Action.Start ) ? ConsoleColor.Blue : default);
				}
				var totalTime = TimeSpan.FromMinutes(day
						.Select(x => x.GetTimeSpan())
						.Sum(x => x.TotalMinutes));

				PrintWithColor.WriteLine($"{day.Key:MMM-dd}" + " Total time: " + totalTime
					, background: day.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
				PrintWithColor.WriteLine("");
			}
			return true;
		}
	}

	public class GetTime : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Get time: ");

			var fileHandler = new FileHandler();

			var request = new CommandRequest()
			{
				Arg = InteruptableReader.ReadLine(),
			};
			var allLogs = fileHandler
				.GetAllLogs(request);
			
			var groupPerNames = allLogs
				.GroupBy(x => x.Name)
				.OrderByDescending(x => x.Min(m => m.Action))
				.ThenBy(x => x.Max(x => x.StopTimestamp))
				;
			foreach(var groupPerName in groupPerNames)
			{
				var groupPerDay = groupPerName
					.GroupBy(x => DateTimeOffset.FromUnixTimeSeconds(x.StartTimestamp).Date);

				foreach (var group in groupPerDay)
				{
					foreach(var log in group)
					{
						PrintWithColor.WriteLine(log.DisplayReadable(x => x.Name, x => x.StartTime, x => x.StopTime) + " time: " + log.GetTimeSpan()
							, background: log.Action == Action.Start ? ConsoleColor.Blue : default);
					}

					if (groupPerDay.Count() > 1)
					{
						var totalTimeForDay = TimeSpan.FromMinutes(group
							.Select(x => x.GetTimeSpan())
							.Sum(x => x.TotalMinutes));

						PrintWithColor.WriteLine($"{group.Key:MMM-dd}"+ " Sum: " + totalTimeForDay
							, background: group.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);
					}
				}

				var totalTime = TimeSpan.FromMinutes(groupPerName
					.Select(x => x.GetTimeSpan())
					.Sum(x => x.TotalMinutes));
				PrintWithColor.WriteLine("Sum: " + totalTime
					, background: groupPerName.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);

				PrintWithColor.WriteLine("");
			}
			return true;
	   }
	}

	public class Rename : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		private int number => 99;
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
		public int TimeoutTime => Timeout.Infinite;
		private int number => 99;
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
				PrintWithColor.WriteLine(i + " : " + logs[i].DisplayReadable()
					, background: logs[i].Action == Action.Start ? ConsoleColor.Blue : default);
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
		public int TimeoutTime => Timeout.Infinite;
		public bool Execute()
	   {
		   
			PrintWithColor.WriteLine("Sure to delete all? type yes to accept");
			if(Console.ReadLine() == "yes")
			{

				var fileHandler = new FileHandler();
				fileHandler.Reset();
			}
			return true;
	   }
	}
	public class RestartRecent : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		static int number => 25;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => x.Name)
				.TakeLast(number)
				.OrderByDescending(x => x.Max( mbox => mbox.StartTimestamp))
				.ToList();

			for(int i = 0; i < Math.Min(logs.Count, number); i++)
			{
				PrintWithColor.WriteLine(i + " : " + logs[i].Key);
			}
			if(int.TryParse(InteruptableReader.ReadLine(), out int index) 
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
	public class CreateNewHistoric : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		
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


			var time = (DateTimeOffset) DateTime.Today.AddHours(3);
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
			list.Execute(new CommandRequest {Arg = name, ExactSearch = true });

			return true;
		}
	}
	public class Merge : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		private static int number => 25;
		public bool Execute()
		{
			var fileHandler = new FileHandler();
			var logs = fileHandler
				.GetAllLogs(new CommandRequest())
				.GroupBy(x => x.Name)
				.TakeLast(number)
				.OrderByDescending(x => x.Max(mbox => mbox.StartTimestamp))
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
				for (int i = 0; i < Math.Min(logs.Count, number); i++)
				{
					if(i != targetIndex)
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
					foreach(var log in logs[targetIndex])
					{
						log.Name = logs[sourceIndex].Key;
					}
					fileHandler.Create(logs[targetIndex]);
				}
			}
			return true;
		}
	}
	public class Invalid : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		public bool Execute()
		{
			PrintWithColor.WriteLine("Invalid operation");
			return true;
		}
	}
	public class Exit : ICommand
	{
		public int TimeoutTime => Timeout.Infinite;
		public bool Execute()
		{
			return false;
		}
	}
}