using System;

#pragma warning disable CS8618

namespace RainMeadowPupifier
{
    public partial class RainMeadowPupifier
    {
        private static string lastErrorMessage;
        private static int lastErrorCount = 0;

        public static void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string log = $"[{timestamp}] {message}";

            instance.Logger.LogInfo(log);
        }

        public static int repetition = 1;
        public static void LogError(Exception exception, string customMessage)
        {
            string errorCore =
                $"Error Message: {customMessage}\n" +
                $"Exception Type: {exception.GetType().Name}\n" +
                $"Error: {exception.Message}\n" +
                $"StackTrace:\n{exception.StackTrace}";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (errorCore == lastErrorMessage)
            {
                lastErrorCount++;

                // Log the message anyway after every count * 2 repetitions
                if (lastErrorCount % repetition == 0)
                {
                    repetition *= 2;
                    instance.Logger.LogInfo(new string('-', 80));
                    instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                    instance.Logger.LogInfo(errorCore);
                    instance.Logger.LogInfo($"The last message has repeated {lastErrorCount} times.");
                    instance.Logger.LogInfo(new string('-', 80));
                }
            }
            else
            {
                // Log the repetition count of the last error if applicable
                if (lastErrorCount > 0)
                {
                    instance.Logger.LogInfo(new string('-', 80));
                    instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                    instance.Logger.LogInfo(errorCore);
                    instance.Logger.LogInfo($"Repetitions: This error has repeated {lastErrorCount} times");
                    instance.Logger.LogInfo(new string('-', 80));
                    lastErrorCount = 0;
                    repetition = 1;
                }

                // Log the new error message

                instance.Logger.LogInfo(new string('-', 80));
                instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                instance.Logger.LogInfo(errorCore);
                instance.Logger.LogInfo(new string('-', 80));

                // Update the last error message
                lastErrorMessage = errorCore;
            }
        }
    }
}
