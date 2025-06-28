using System;

#pragma warning disable CS8618

namespace Pupifier
{
    public partial class Pupifier
    {
        private static string _lastErrorMessage;
        private static int _lastErrorCount = 0;

        public static void Log(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var log = $"[{timestamp}] {message}";

            Instance.Logger.LogInfo(log);
        }

        public static int Repetition = 1;
        public static void LogError(Exception exception, string customMessage)
        {
            var errorCore =
                $"Error Message: {customMessage}\n" +
                $"Exception Type: {exception.GetType().Name}\n" +
                $"Error: {exception.Message}\n" +
                $"StackTrace:\n{exception.StackTrace}";
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            if (errorCore == _lastErrorMessage)
            {
                _lastErrorCount++;

                // Log the message anyway after every count * 2 repetitions
                if (_lastErrorCount % Repetition == 0)
                {
                    Repetition *= 2;
                    Instance.Logger.LogInfo(new string('-', 80));
                    Instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                    Instance.Logger.LogError(exception);
                    Instance.Logger.LogInfo($"The last message has repeated {_lastErrorCount} times.");
                    Instance.Logger.LogInfo(new string('-', 80));
                }
            }
            else
            {
                // Log the repetition count of the last error if applicable
                if (_lastErrorCount > 0)
                {
                    Instance.Logger.LogInfo(new string('-', 80));
                    Instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                    Instance.Logger.LogError(exception);
                    Instance.Logger.LogInfo($"Repetitions: This error has repeated {_lastErrorCount} times");
                    Instance.Logger.LogInfo(new string('-', 80));
                    _lastErrorCount = 0;
                    Repetition = 1;
                }

                // Log the new error message

                Instance.Logger.LogInfo(new string('-', 80));
                Instance.Logger.LogInfo($"[{timestamp}] An error has occurred!");
                Instance.Logger.LogError(exception);
                Instance.Logger.LogInfo(new string('-', 80));

                // Update the last error message
                _lastErrorMessage = errorCore;
            }
        }
    }
}
