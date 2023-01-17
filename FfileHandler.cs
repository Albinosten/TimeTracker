using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;

namespace TimeTrackerApp
{
    public class FileHandler
    {
        static string path => "Log.csv";
        
        public bool Create(Log line)
        {
            if(!File.Exists(path))
            {
                File.Create(path).Close();
            }
            var request = new CommandRequest()
            {
                Arg = line.Name,
                ExactSearch = true,
            };
            var logs = this.GetAllLogs(request);
            if(!logs.Any(x => x.Action == Action.Start))
            {
                var file = File.Open(path, FileMode.Append);
                var streamWriter = new StreamWriter(file);

                streamWriter.WriteLine(line);
            
                streamWriter.Close();
                file.Close();
                return true;
            }
            return false;
        }
        public void Delete(Log log)
        {
            if(!File.Exists(path))
            {
                File.Create(path).Close();
            }
            var a = this.GetAllLogs(new CommandRequest())
                .Where(l => !l.Equals(log))
                .Select(x => x.ToString())
                .ToList();
            File.WriteAllLines(path, a
                );
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