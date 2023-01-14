using System;
using System.Linq;

namespace TimeTrackerApp
{
    public class Start : ICommand
    {
       public void Execute()
       {
           Console.WriteLine("Start name: ");
            var fileHandler = new FileHandler();
            var name = Console.ReadLine();
            if(string.IsNullOrEmpty(name)) throw new Exception("Must set a name");
            var log = new Log
            {
                StartTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                Action = Action.Start,
                Name = name,
            };
            fileHandler.Create(log);   
       }
    }
    public class Stop : ICommand
    {
       public void Execute()
       {
           Console.WriteLine("Stop: ");
            var fileHandler = new FileHandler();
            var logs = fileHandler
                .GetAllLogs(Console.ReadLine())
                .Where(x => x.Action == Action.Start)
                .ToList();
            foreach(var log in logs)
            {
                fileHandler.Delete(log);
                log.StopTimestamp =  DateTimeOffset.Now.ToUnixTimeSeconds();
                log.Action = Action.Stop;
                fileHandler.Create(log);
            }
       }
    }
    public class List : ICommand
    {
       public void Execute()
       {
           Console.WriteLine("List: ");

            var fileHandler = new FileHandler();
            var lines = fileHandler.GetAllLines(Console.ReadLine());
            foreach(var line in lines)
            {
                Console.WriteLine(line);
            }
       }
    }
    public class ListActive : ICommand
    {
       public void Execute()
       {
            var fileHandler = new FileHandler();
            var lines = fileHandler
                .GetAllLogs(null)
                .Where(x => x.Action == Action.Start)
                .ToList()
                ;
            foreach(var line in lines)
            {
                Console.WriteLine(line);
            }
       }
    }
    public class GetTime : ICommand
    {
       public void Execute()
       {
           Console.WriteLine("Get time: ");

            var fileHandler = new FileHandler();
            var allLogs = fileHandler
                .GetAllLogs(Console.ReadLine());

            var grouping = allLogs
                .GroupBy(x => x.Name);
            foreach(var group in grouping)
            {
                var totalTime = TimeSpan.FromMinutes(group
                    .Where(x => x.Action == Action.Stop)
                    .Select(x => x.GetTimeSpan())
                    .Sum(x => x.TotalMinutes));

                foreach(var log in group)
                {
                    Console.WriteLine(log);
                }
                Console.WriteLine((int)totalTime.Hours + ":" + (int)totalTime.Minutes);
            }
       }
    }
    public class Reset : ICommand
    {
       public void Execute()
       {
            Console.WriteLine("Sure to delete all? type yes to accept");
            if(Console.ReadLine() == "yes")
            {

            var fileHandler = new FileHandler();
            fileHandler.Reset();
            }
       }
    }
    public class RestartRecent : ICommand
    {
        static int number => 10;
        public void Execute()
        {
            var fileHandler = new FileHandler();
            var logs = fileHandler
                .GetAllLogs(null)
                .TakeLast(number)
                .ToList();

            for(int i = 0; i<Math.Min(logs.Count, number); i++)
            {
                Console.WriteLine(i + " : " + logs[i].Name);
            }
            if(int.TryParse(Console.ReadLine(), out int index) 
                && index < Math.Min(logs.Count, number) 
                && index >= 0
                )
            {
                var log = new Log
                {
                    StartTimestamp = DateTimeOffset.Now.ToUnixTimeSeconds(),
                    Action = Action.Start,
                    Name = logs[index].Name,
                };
                fileHandler.Create(log);   
            }
        }
    }
}