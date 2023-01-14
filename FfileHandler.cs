using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace TimeTrackerApp
{
    public class FileHandler
    {
        string path => "Log.csv";
        public bool Create(Log line)
        {
            if(!File.Exists(path))
            {
                File.Create(path).Close();
            }
            var logs = this.GetAllLogs(line.Name);
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
            var a = this.GetAllLogs(null)
                .Where(l => !l.Equals(log))
                .Select(x => x.ToString())
                .ToList();
            File.WriteAllLines(path, a
                );
        }
        public IList<Log> GetAllLogs(string args)
        {
            return this.GetAllLines(args)
                .Select(x => Log.Parse(x))
                .ToList();
        }
        public IList<string> GetAllLines(string args)
        {
            if(!File.Exists(path))
            {
                return new List<string>();
            }
            return File.ReadAllLines(path)
                .Where(x => string.IsNullOrEmpty(args) ? true : args == Log.Parse(x).Name)
                .ToList();
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