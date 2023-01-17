using System;
using System.Collections.Generic;
using System.Linq;
namespace TimeTrackerApp
{
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
    }
    public class TimeTracker
    {
        static void Main(string[] args)
        {
#if DEBUG

#endif
            bool run = true;
            Console.Clear();
			while (run) 
            {
                Console.WriteLine("Create new 1:");
                Console.WriteLine("Stop 2:");
                Console.WriteLine("List 3:");
                Console.WriteLine("List active 4:");
                Console.WriteLine("Get Time 5:");
                Console.WriteLine("Restart recent 6:");
                //Console.WriteLine("Reset file RESET:");
                Console.WriteLine("Exit x:");
                var command = GetCommand(GetAlias(Console.ReadLine()));
                Console.Clear();
                run = command.Execute();
                Console.WriteLine("*************************************");
            }
        }
        private static ICommand GetCommand(int a) => a switch
        {
            1 => new Start(),
            2 => new Stop(),
            3 => new List(),
            4 => new ListActive(),
            5 => new GetTime(),
            6 => new RestartRecent(),
            //95 => new MergeTogether(),
            //96 => new Rename(),
            97 => new Remove(),
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
            "X" => 99,
            "START" => 1,
            "S" => 1,
            "Q" => 2,
            "QUIT" => 2,
            "STOP" => 2,
            "END" => 2,
            "LIST" => 3,
            "L" => 3,
            "LS" => 3,
            "G" => 5,
            "GET" => 5,
            "TIME" => 5,
            "REMOVE" => 97,
            "DELETE ALL" => 98,
            _ => -1,
        };
    }
}
