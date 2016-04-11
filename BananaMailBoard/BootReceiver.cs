using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Util;

namespace BananaMailBoard
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Android.Content.Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        const string TAG = "BananaMailBoard";

        static readonly TimeSpan INTERVAL = TimeSpan.FromMinutes(1);

        public override void OnReceive(Context context, Intent intent)
        {
            Log.Debug(TAG, "BootReceiver.OnReceive");

            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            alarmManager.SetRepeating(
                AlarmType.RtcWakeup,
                SystemClock.ElapsedRealtime() + (long)INTERVAL.TotalMilliseconds, 
                (long)INTERVAL.TotalMilliseconds, 
                PendingIntent.GetService(context, 0, new Intent(context, typeof(MailReceiveService)), PendingIntentFlags.CancelCurrent));
        }
    }
}