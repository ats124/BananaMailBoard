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
    using Util;

    [BroadcastReceiver]
    [IntentFilter(new[] { Android.Content.Intent.ActionBootCompleted }, Categories = new[] { Android.Content.Intent.CategoryDefault })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            LogHelper.Debug("START");
            try
            {
                AlarmReceiver.SetDoMailReceiveAlarm(context, false);
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "SetDoMailReceiveAlarm Error");
            }
            LogHelper.Debug("END");
        }
    }
}