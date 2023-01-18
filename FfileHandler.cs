using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TimeTrackerApp
{
	public class FileHandler
	{
		static string path => "Log.csv";
		
		/// <summary>
		/// Only possible to set one name
		/// </summary>
		/// <param name="lines"></param>
		/// <returns></returns>
		public bool Create(IList<Log> lines)
		{
			if (!File.Exists(path))
			{
				File.Create(path).Close();
			}
			var request = new CommandRequest()
			{
				Arg = lines[0].Name,
				ExactSearch = true,
			};
			var logs = this.GetAllLogs(request);
			if (!logs.Any(x => x.Action == Action.Start))
			{
				var file = File.Open(path, FileMode.Append);
				var streamWriter = new StreamWriter(file);

				foreach(var line in lines)
				{
					streamWriter.WriteLine(line);
				}

				streamWriter.Close();
				file.Close();
				return true;
			}
			return false;
		}
		public bool Create(Log line)
		{
			return this.Create(new[] { line });
		}
		public void Delete(IList<Log> logs)
		{

			if (!File.Exists(path))
			{
				File.Create(path).Close();
			}
			var a = this.GetAllLogs(new CommandRequest())
				.Where(l => !logs.Contains(l))
				.Select(x => x.ToString())
				.ToList();
			File.WriteAllLines(path, a
				);
		}
		public void Delete(Log log)
		{
			this.Delete(new Log[] { log });
		}
		public IList<Log> GetAllLogs(ICommandRequest request)
		{
			return this.GetAllLines(request)
				.Select(x => Log.Parse(x))
				.ToList();
		}
		public IList<string> GetAllLines(ICommandRequest request)
		{
			if(!File.Exists(path))
			{
				return new List<string>();
			}
			return File.ReadAllLines(path)
				.Where(x => Filter(request, x))
				.ToList();
		}
		private static bool Filter(ICommandRequest request, string line) 
		{
			if (!string.IsNullOrEmpty(request.Arg)) 
			{
				if (request.ExactSearch) 
				{
					return Log.Parse(line).Name.ToUpper() == request.Arg.ToUpper();
				}
				return Log.Parse(line).Name.ToUpper().Contains(request.Arg.ToUpper());
			}
			return true;
		}
		public void Reset()
		{
			if(File.Exists(path))
			{
				File.Delete(path);
			}
		}
	}
}