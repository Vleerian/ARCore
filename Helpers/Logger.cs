using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

using Console = Colorful.Console;

namespace ARCore.Helpers
{
    public enum LogEventType
    {
        Fatal,
        Error,
        Warning,
        Information,
        Debug,
        Verbose,
        None
    }

    public static class Logger
    {
        public static LogEventType logLevel;

        public static void Log(LogEventType logEventType, string Message, Exception exception = null)
        {
            if (logEventType > logLevel)
                return;

            Color statusColor = Color.White;
            switch (logEventType)
            {
                case LogEventType.Fatal:
                case LogEventType.Error:
                    statusColor = Color.Red;
                    break;
                case LogEventType.Warning:
                    statusColor = Color.Yellow;
                    break;
                case LogEventType.Information:
                    statusColor = Color.Cyan;
                    break;
                case LogEventType.Debug:
                    statusColor = Color.Orange;
                    break;
            }


            string ExceptionLine = (exception != null) ? $" - {exception.ToString()}" : "";
            Console.WriteLineFormatted($"[{DateTime.Now.ToString("dd'/'MM'/'yyyy HH:mm:ss")}] <{{0}}> {Message}{ExceptionLine}", statusColor, Color.White, logEventType.ToString().Center(9));
            if (exception != null && logLevel == LogEventType.Debug)
                Console.WriteLine(exception.StackTrace);
        }
    }
}
