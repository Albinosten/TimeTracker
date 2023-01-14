using System;
using System.Collections.Generic;
using System.Linq;
namespace TimeTrackerApp
{
    public interface ICommand
    {
        void Execute();
    }
    public class TimeTracker
    {
        static void Main(string[] args)
        {
            #if DEBUG
            // args = new []{"stop","all"};
            // args = new []{"s","albin" , "Nya", "Branch"};
            #endif
            Console.Clear();
            Console.WriteLine("Create new 1:");
            Console.WriteLine("Stop 2:");
            Console.WriteLine("List 3:");
            Console.WriteLine("List active 4:");
            Console.WriteLine("Get Time 5:");
            Console.WriteLine("Reset file 6:");
            Console.WriteLine("Restart recent 7:");
            var command = GetCommand(GetAlias(Console.ReadLine()));
            command.Execute();
        }
        private static ICommand GetCommand(int a) => a switch
        {
            1 => new Start(),
            2 => new Stop(),
            3 => new List(),
            4 => new ListActive(),
            5 => new GetTime(),
            6 => new Reset(),
            7 => new RestartRecent(),
            _ => throw new InvalidOperationException(),
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
            "G" => 5,
            "GET" => 5,
            "TIME" => 5,
            "RESET" => 6,
            "CLEAR" => 6,
            _ => throw new InvalidOperationException(c),
        };
    }
}
