using System;

namespace TimeTrackerApp
{
	public static class PrintWithColor
	{
		public static void WriteLine(string text, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White)
		{
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.WriteLine(text);
			Console.ResetColor();
		}
		public static void Write(string text, ConsoleColor background = ConsoleColor.Black, ConsoleColor foreground = ConsoleColor.White)
		{
			Console.BackgroundColor = background;
			Console.ForegroundColor = foreground;
			Console.Write(text);
			Console.ResetColor();
		}
	}
}
