using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TimeTrackerApp
{
	public class FileHandler
	{
		static string path => Location + name;
		public static string Location => "";
		// public static string Location => "C:\\src\\Files\\";
		static string name => "Log.csv";

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
		public bool Create(IEnumerable<Log> logs)
		{
			return this.Create(logs.ToList());
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
		public void Delete(IEnumerable<Log> log)
		{
			this.Delete(log.ToList());
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
		public string Backup()
		{
			var newName = Location +  "Log " + $"{DateTimeOffset.Now.LocalDateTime:yyyy-MM-dd HH-mm-ss}" +  ".csv";
			if (!File.Exists(newName))
			{
				File.Create(newName).Close();
			}
			var allRows = this.GetAllLines(new CommandRequest());
			
			var file = File.Open(newName, FileMode.Append);
			var streamWriter = new StreamWriter(file);

			foreach (var line in allRows)
			{
				streamWriter.WriteLine(line);
			}

			streamWriter.Close();
			file.Close();
			return newName;
		}
		public List<string> GetAllFiles()
		{
			return Directory.GetFiles(Location).ToList();
		}
		public long GetFileSize(string fileName)
		{
			if (File.Exists(Location + fileName))
			{
				return new FileInfo(Location + fileName).Length;
			}
			return 0;
		}
		public void Restore(string restoreFrom)
		{
			try
			{
				var allLines = File
					.ReadAllLines(Location + restoreFrom)
					.Select(Log.Parse)
					.ToList();
					this.Reset();
					this.Create(allLines);
					PrintWithColor.WriteLine("Restored from: " + restoreFrom);
			}
			catch
			{
				PrintWithColor.WriteLine("Could not restore from: " + restoreFrom);
			}
		}
	}
}