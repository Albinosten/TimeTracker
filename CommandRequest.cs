namespace TimeTrackerApp
{
	public class CommandRequest : ICommandRequest
	{
		public string Arg { get; set; }
		public bool ExactSearch { get; set; }
		public bool Clear { get; set; }
	}
}