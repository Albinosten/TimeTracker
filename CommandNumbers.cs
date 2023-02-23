namespace TimeTrackerApp
{
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
		Week = 8,
		Commands = 9,
		Restore = 88,
		Backup = 89,
		EditStartTime = 90,
		EditEndTime = 91,
		Merge = 92,
		NewHistoric = 95,
		Rename = 96,
		Delete = 97,
		Reset = 98,
		Exit = 99,
	}

	public static class CommandNumberHelper
	{
		public static string GetHelpText(this CommandNumbers command) => command switch
		{
			CommandNumbers.Invalid => "",
			CommandNumbers.Start => "",//"Start a new log",
			CommandNumbers.Stop => "Stop logs. All by default",
			CommandNumbers.List => "List all logs",
			CommandNumbers.Active => "Show all ongoing logs",
			CommandNumbers.GetTime => "Get total time",
			CommandNumbers.Day => "Sum of all entries per day",
			CommandNumbers.Restart => "Start a new log with name suggestions",
			CommandNumbers.Week => "Sum of all entries per week",
			CommandNumbers.Commands => "Show all commands",
			CommandNumbers.Restore => "Restores data from backup file",
			CommandNumbers.Backup => "Creates a backup from current data",
			CommandNumbers.EditStartTime => "Add time by moving start timestamp",
			CommandNumbers.EditEndTime => "Add time by moving End timestamp",
			CommandNumbers.Merge => "Merges two names into one",
			CommandNumbers.NewHistoric => "Creates a historic log entrie",
			CommandNumbers.Rename => "Rename all log entries with name",
			CommandNumbers.Delete => "Delete a log entry",
			CommandNumbers.Reset => "Reset logfile and alla data",
			CommandNumbers.Exit => "Close app",
			_ => "",
		};
	}
}