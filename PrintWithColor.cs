using System;

namespace TimeTrackerApp
{
	public static class PrintWithColor
	{
		public static void WriteLine(string text, ConsoleColor? background = default, ConsoleColor? foreground = default)
		{
			Console.BackgroundColor = background ?? Console.BackgroundColor;
			Console.ForegroundColor = foreground ?? Console.ForegroundColor;
			Console.WriteLine(text);
			Console.ResetColor();
		}
		public static void Write(string text, ConsoleColor? background = default, ConsoleColor? foreground = default)
		{
			Console.BackgroundColor = background ?? Console.BackgroundColor;
			Console.ForegroundColor = foreground ?? Console.ForegroundColor;
			Console.Write(text);
			Console.ResetColor();
		}
	}
}
