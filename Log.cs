using System;
using System.Linq;

namespace TimeTrackerApp
{
	public enum Action
	{
		Start = 0,
		Stop = 1,
	}
	public class Log
	{
		public Action Action;
		public string Name;
		
		public long StartTimestamp;
		public string StartDate => $"{DateTimeOffset.FromUnixTimeSeconds(this.StartTimestamp).LocalDateTime:MMM-dd}";
		public string StartTime=> $"{DateTimeOffset.FromUnixTimeSeconds(this.StartTimestamp).LocalDateTime:HH:mm:ss}";
		
		public long StopTimestamp;
		public string StopDate => this.StopTimestamp == 0L ? "--:--" : $"{DateTimeOffset.FromUnixTimeSeconds(this.StopTimestamp).LocalDateTime:MMM-dd}";
		public string StopTime => this.StopTimestamp == 0L ? "--:--:--" : $"{DateTimeOffset.FromUnixTimeSeconds(this.StopTimestamp).LocalDateTime:HH:mm:ss}";

		public override string ToString()
		{
			return string.Join(", ", this.Name, (int)this.Action, this.StartTimestamp, this.StopTimestamp);
		}
		public string DisplayReadable(bool showAction = false)
		{
			if (showAction)
			{
				return string.Join(", "
				, this.Name
				, this.Action
				, $"{this.StartDate} {this.StartTime}"
				, this.StopTimestamp == 0L ? "--:--:--" : StopDate
				);
			}
			return string.Join(", "
				, this.Name
				, $"{this.StartDate} {this.StartTime}"
				, this.StopTimestamp == 0L ? "--:--:--" : $"{this.StopDate} {this.StopTime}"
				);
		}
		public string DisplayReadable(params Func<Log,object>[] properties)
		{
			return string.Join(", ", properties.Select(x => x.Invoke(this)));
		}
		public TimeSpan GetTimeSpan()
		{
			return DateTimeOffset.FromUnixTimeSeconds(this.StopTimestamp == 0L ? DateTimeOffset.Now.ToUnixTimeSeconds() : this.StopTimestamp)
				 - DateTimeOffset.FromUnixTimeSeconds(this.StartTimestamp);
		}
		public static Log Parse(string s)
		{
			var values = s.Split(", ");
			return new Log
			{
				Name = values[0],
				Action = (Action)int.Parse( values[1]),
				StartTimestamp = long.Parse(values[2]),
				StopTimestamp = long.Parse(values[3]),
			};
		}
		public override bool Equals(object obj)
		{
			var other = (Log)obj;
			return this.Name == other.Name
				&& this.Action == other.Action
				&& this.StartTimestamp == other.StartTimestamp
				&& this.StopTimestamp == other.StopTimestamp
				;
		}
		public override int GetHashCode()
		{
			return base.GetHashCode();
		}
	}
}