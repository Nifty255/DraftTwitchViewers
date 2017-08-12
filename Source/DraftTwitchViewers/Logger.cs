using System.IO;
using UnityEngine;

namespace DraftTwitchViewers
{
    class Logger
    {
        public static void DebugLog(string text)
        {
            Log.Info("(Log): " + text);
        }

        public static void DebugWarning(string text)
        {
            Log.Warning("(Warning): " + text);
        }

        public static void DebugError(string text)
        {
            Log.Error("(ERROR): " + text);
        }

        public static void LogToFile(string text, bool asLines)
        {
            try
            {
                if (asLines)
                {
                    string[] lines = text.Split("\n".ToCharArray());

                    File.WriteAllLines(@"C:\DTV Log.txt", lines);
                }
                else
                {
                    File.WriteAllText(@"C:\DTV Log.txt", text);
                }
            }
            catch
            {

            }
        }
    }
}
