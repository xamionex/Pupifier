using System;
using System.ComponentModel;
using System.IO;

namespace RainMeadowPupifier
{
    public partial class RainMeadowPupifier
    {
        private static readonly string logFilePath = Path.Combine(Environment.CurrentDirectory, "RainMeadowPupifier.log");

        public static void Log(string message, bool date = true)
        {
            if (date)
                message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} {message}";

            File.AppendAllText(logFilePath, message + Environment.NewLine);
            instance.Logger.LogInfo(message);
        }

        public static void LogError(Exception ex, string? ErrorMessage = null)
        {
            Log(new string('-', 80), false);
            Log("An error has occured!");
            if (ErrorMessage != null)
            {
                Log($"Error Message: {ErrorMessage}");
            }
            Log($"Exception Type: {ex.GetType().Name}", false);
            Log($"Error: {ex.Message}", false);
            Log($"StackTrace:\n{ex.StackTrace}", false);
            if (ex.InnerException != null)
            {
                Log("Inner exception: ", false);
                LogError(ex.InnerException);
            }
            Log(new string('-', 80), false);
        }
    }
}
