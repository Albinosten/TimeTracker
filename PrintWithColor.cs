using System;

namespace TimeTrackerApp
{
	public static class PrintWithColor
	{
		public static void PrintLine(string text, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White)
		{
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.WriteLine(text);
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
		}
		public static void Print(string text, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White)
		{
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.Write(text);
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
		}
	}
}
