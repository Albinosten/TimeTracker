using System;
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
        public long StopTimestamp;
        public override string ToString()
        {
            return string.Join(", ", this.Name, (int)this.Action, this.StartTimestamp, this.StopTimestamp);
        }
        public TimeSpan GetTimeSpan()
        {
            return DateTimeOffset.FromUnixTimeSeconds(this.StopTimestamp)
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