using System;
using System.Threading;


namespace TimeTrackerApp
{
   public class InteruptableReader
    {
        private static Thread inputThread;
        private static AutoResetEvent getInput, gotInput;
        private static string input;

        static InteruptableReader()
        {
            getInput = new AutoResetEvent(false);
            gotInput = new AutoResetEvent(false);
			inputThread = new Thread(reader)
			{
				IsBackground = true
			};
			inputThread.Start();
        }

        private static void reader()
        {
            while (true)
            {
                getInput.WaitOne();
                input = Console.ReadLine().Sanitize();
                gotInput.Set();
            }
        }

        // omit the parameter to read a line without a timeout
        public static bool TryReadLine(out string result, int timeOutMillisecs = Timeout.Infinite)
		{
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
			{
                result = input;
	    		return true;
    		}

			else
			{
                result = string.Empty;
                return false;
			}

        }
        public static string ReadLine(int timeOutMillisecs = Timeout.Infinite)
        {
            getInput.Set();
            bool success = gotInput.WaitOne(timeOutMillisecs);
            if (success)
                return input;
            else
                throw new TimeoutException("User did not provide input within the timelimit.");
        }
    }
}
