using System;
using System.Linq;
using System.Collections.Generic;

namespace TimeTrackerApp
{
    public class CommandRequest : ICommandRequest
    {
        public string Arg { get; set; }
        public bool ExactSearch { get; set; }
    }
    public class Start : ICommandWithArg
    {
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
           Console.WriteLine("Start name: ");
            var request = new CommandRequest() { Arg = Console.ReadLine() };
            if (string.IsNullOrEmpty(request.Arg)) 
            {
                //throw new Exception("Must set a name")
                Console.WriteLine("Must set a name");
                return true;
            };
            this.Execute(request);

            return true;
       }
    }
    public class Stop : ICommand
    {
        private int number => 99;
       public bool Execute()
       {
           Console.WriteLine("Stop: ");
            var fileHandler = new FileHandler();
            var logs = fileHandler
                .GetAllLogs(new CommandRequest())
                .Where(x => x.Action == Action.Start)
                .TakeLast(number)
                .GroupBy(x => x.Name)
                .ToList();
            var logsToRemove = new List<Log>();

            PrintWithColor.PrintLine("Enter for all", ConsoleColor.DarkRed);
            for (int i = 0; i < Math.Min(number, logs.Count); i++)
            {
                Console.WriteLine(i + " : " + logs[i].Key);
            }
            PrintWithColor.PrintLine("x for cancel", ConsoleColor.DarkRed);

            var input = Console.ReadLine();
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
        protected IEnumerable<Log> GetLogs(ICommandRequest request)
        {
            var fileHandler = new FileHandler();
            return fileHandler.GetAllLogs(request);
        }
        protected void Print(IList<Log> logs)
        {
            foreach (var log in logs)
            {
                PrintWithColor.Print(log.DisplayReadable());
                PrintWithColor.PrintLine(" time: " + log.GetTimeSpan()
                    , background: log.Action == Action.Start ? ConsoleColor.Blue: default);
            }
            Console.WriteLine("Count: " + logs.Count);
        }
        public bool Execute(ICommandRequest request)
		{
            this.Print(this.GetLogs(request).ToList());
            return true;
		}
       public virtual bool Execute()
       {
            Console.WriteLine("Name: ");
            var request = new CommandRequest()
            {
                Arg = Console.ReadLine(),
            };
            this.Execute(request);
            return true;
       }
    }
    public class ListActive : List
    {
       public override bool Execute()
       {
            var lines = this.GetLogs(new CommandRequest())
                .Where(x => x.Action == Action.Start)
                .ToList();
            this.Print(lines);
            return true;
       }
    }
    public class GetTime : ICommand
    {
       public bool Execute()
       {
           Console.WriteLine("Get time: ");

            var fileHandler = new FileHandler();

            var request = new CommandRequest()
            {
                Arg = Console.ReadLine(),
            };
            var allLogs = fileHandler
                .GetAllLogs(request);

            var grouping = allLogs
                .GroupBy(x => x.Name)
                .OrderByDescending(x => x.Min(m => m.Action))
                .ThenBy(x => x.Max(x => x.StopTimestamp))
                ;
            foreach(var group in grouping)
            {
                var totalTime = TimeSpan.FromMinutes(group
                    .Select(x => x.GetTimeSpan())
                    .Sum(x => x.TotalMinutes));

                foreach(var log in group)
                {
                    PrintWithColor.PrintLine(log.DisplayReadable() + " time: " + log.GetTimeSpan()
                        , background: log.Action == Action.Start ? ConsoleColor.Blue : default);
                }

				PrintWithColor.PrintLine("Sum: " + totalTime
					, background: group.Any(x => x.Action == Action.Start) ? ConsoleColor.DarkMagenta : ConsoleColor.DarkGreen);

				Console.WriteLine();
            }
            return true;
       }
    }

    public class Remove : ICommand
	{
        private int number => 99;
        public bool Execute()
		{
            var fileHandler = new FileHandler();
            Console.WriteLine("Remove entry with name:");
            var logs = fileHandler
                .GetAllLogs(new CommandRequest() { Arg = Console.ReadLine() })
                .TakeLast(number)
                .ToList();

            for (int i = 0; i < Math.Min(number, logs.Count); i++)
            {
                PrintWithColor.PrintLine(i + " : " + logs[i]
                    , background: logs[i].Action == Action.Start ? ConsoleColor.Blue : default);
            }
            PrintWithColor.PrintLine("Any key to return.", ConsoleColor.DarkRed);
            if (int.TryParse(Console.ReadLine(), out int index)
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
       public bool Execute()
       {
            Console.WriteLine("Sure to delete all? type yes to accept");
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
                Console.WriteLine(i + " : " + logs[i].Key);
            }
            if(int.TryParse(Console.ReadLine(), out int index) 
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
    public class Invalid : ICommand
    {
        public bool Execute()
        {
            Console.WriteLine("Invalid operation");
            return true;
        }
    }
    public class Exit : ICommand
    {
        public bool Execute()
        {
            return false;
        }
    }
}