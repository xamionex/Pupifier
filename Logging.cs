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

        public static void LogError(Exception exception, string customMessage)
        {
            string errorCore =
                $"Error Message: {customMessage}\n" +
                $"Exception Type: {exception.GetType().Name}\n" +
                $"Error: {exception.Message}\n" +
                $"StackTrace:\n{exception.StackTrace}";
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            string errorDetails =
                "--------------------------------------------------------------------------------\n" +
                $"[{timestamp}] An error has occurred!\n" +
                errorCore + "\n" +
                "--------------------------------------------------------------------------------";

            if (errorCore == lastErrorMessage)
            {
                lastErrorCount++;

                // Log the message after every 100 repetitions
                if (lastErrorCount % 100 == 0)
                {
                    instance.Logger.LogInfo(errorDetails);
                    Log($"The last message has repeated {lastErrorCount} times.");
                }
            }
            else
            {
                // Log the repetition count of the last error if applicable
                if (lastErrorCount > 0)
                {
                    string lastErrorDetails =
                        "--------------------------------------------------------------------------------\n" +
                        $"[{timestamp}] An error has occurred!\n" +
                        lastErrorMessage + "\n" +
                        "--------------------------------------------------------------------------------";
                    instance.Logger.LogInfo(lastErrorDetails);
                    Log($"The last message repeated {lastErrorCount} times");
                    lastErrorCount = 0;
                }

                // Log the new error message

                instance.Logger.LogInfo(errorDetails);

                // Update the last error message
                lastErrorMessage = errorCore;
            }
        }
    }
}
