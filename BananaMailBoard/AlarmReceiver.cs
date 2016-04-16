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
using Android.Support.V4.Content;

namespace BananaMailBoard
{
    [BroadcastReceiver]
    [IntentFilter(new[] { Constants.INTENT_DO_MAIL_RECEIVE_ACTION })]
    public class AlarmReceiver : WakefulBroadcastReceiver
    {
        public static void SetDoMailReceiveAlarm(Context context, bool isImmediate)
        {
            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            alarmManager.SetRepeating(
                AlarmType.RtcWakeup,
                SystemClock.ElapsedRealtime() + (isImmediate ? 0 : (long)Constants.DO_MAIL_RECEIVE_INTERVAL.TotalMilliseconds),
                (long)Constants.DO_MAIL_RECEIVE_INTERVAL.TotalMilliseconds,
                PendingIntent.GetBroadcast(context, 0, new Intent(Constants.INTENT_DO_MAIL_RECEIVE_ACTION), PendingIntentFlags.CancelCurrent));
        }

        public override void OnReceive(Context context, Intent intent)
        {
            StartWakefulService(context, new Intent(context, typeof(MailReceiveService)));
        }
    }
}