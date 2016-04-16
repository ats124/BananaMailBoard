using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace BananaMailBoard
{
    using Util;

    [Application]
    public class MainApplication : Application
    {
        public MainApplication()
        {

        }

        protected MainApplication(IntPtr javaReference, JniHandleOwnership transfer)
            : base(javaReference, transfer)
        {
        }

        /// <summary>
        /// 最後に表示したメッセージUID。
        /// </summary>
        internal string LastViewMessageUid { get; set; }

        /// <summary>
        /// 現在のメインアクティビティインスタンス。
        /// </summary>
        internal MainActivity CurrentMainActivity { get; set; }

        public override void OnCreate()
        {
            base.OnCreate();
        }
        
        internal void OnReceiveMail(Bundle message)
        {
            // UIスレッドに同期して実行
            if (!MainLooper.IsCurrentThread)
            {
                SynchronizationContext.Send(o => OnReceiveMail((Bundle)o), message);
                return;
            }

            LogHelper.Debug("START");

            try
            {
                // 前回表示メッセージと異なっていればメール表示アクティビティを起動
                var messageUid = message.GetString(Constants.BUNDLE_MAIL_MESSAGE_UID);
                if (messageUid != LastViewMessageUid)
                {
                    LogHelper.Debug("Show MailView Activity");
                    CurrentMainActivity?.Finish();
                    StartWakefulMailViewActivity(message);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }

            LogHelper.Debug("END");
        }

        private void StartWakefulMailViewActivity(Bundle message)
        {
            var intent = new Intent(this, typeof(MailViewActivity));
            intent.SetFlags(ActivityFlags.NewTask);
            intent.PutExtras(message);
            StartActivity(intent);
        }
    }
}