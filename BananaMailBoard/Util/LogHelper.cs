using System;
using System.Runtime.CompilerServices;

using Android.Util;

namespace BananaMailBoard.Util
{
    public static class LogHelper
    {
        public const string LOG_TAG = "BananaMailBoard";

        public static void Debug(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            if (Log.IsLoggable(LOG_TAG, LogPriority.Debug))
                Log.Debug(LOG_TAG, $"{file}:{line} - {member}: {msg}");
        }

        public static void Debug(IFormattable f, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            if (Log.IsLoggable(LOG_TAG, LogPriority.Debug))
                Log.Debug(LOG_TAG, $"{file}:{line} - {member}: {f}");
        }

        public static void Error(string msg, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            Log.Error(LOG_TAG, $"{file}:{line} - {member}: {msg}");
        }

        public static void Error(IFormattable f, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            Log.Error(LOG_TAG, $"{file}:{line} - {member}: {f}");
        }

        public static void Error(Exception ex, string msg = "", [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            Log.Error(LOG_TAG, Java.Lang.Throwable.FromException(ex), $"{file}:{line} - {member}: {msg}");
        }
        public static void Error(Exception ex, IFormattable f, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0, [CallerMemberName] string member = "")
        {
            Log.Error(LOG_TAG, Java.Lang.Throwable.FromException(ex), $"{file}:{line} - {member}: {f}");
        }
    }
}