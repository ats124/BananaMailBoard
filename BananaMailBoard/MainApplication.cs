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
        /// �Ō�ɕ\���������b�Z�[�WUID�B
        /// </summary>
        internal string LastViewMessageUid { get; set; }

        /// <summary>
        /// ���݂̃��C���A�N�e�B�r�e�B�C���X�^���X�B
        /// </summary>
        internal MainActivity CurrentMainActivity { get; set; }

        public override void OnCreate()
        {
            base.OnCreate();
        }
        
        internal void OnReceiveMail(Bundle message)
        {
            // UI�X���b�h�ɓ������Ď��s
            if (!MainLooper.IsCurrentThread)
            {
                SynchronizationContext.Send(o => OnReceiveMail((Bundle)o), message);
                return;
            }

            LogHelper.Debug("START");

            try
            {
                // �O��\�����b�Z�[�W�ƈقȂ��Ă���΃��[���\���A�N�e�B�r�e�B���N��
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