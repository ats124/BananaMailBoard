using System;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;

using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Media;
using Android.Util;
using Android.App;
using Android.Preferences;

using SQLite;

namespace BananaMailBoard
{
    using Entity;

    public class MailViewFragment : BaseFragment
    {
        static MailViewFragment()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                return true;
            };
        }

        private static SoundPool spMessageAlertTone = null;
        private static int soundId = 0;
        private static bool soundStop = false;
        private static PowerManager.WakeLock wakeLock = null;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MailView, container, false);
            view.FindViewById<TextView>(Resource.Id.txtSubject).Text = this.Arguments.GetString(Constants.BUNDLE_MAIL_SUBJECT);
            view.FindViewById<TextView>(Resource.Id.txtBody).Text = this.Arguments.GetString(Constants.BUNDLE_MAIL_BODY);
            var buttonsLayout = view.FindViewById<LinearLayout>(Resource.Id.buttonsLayout);
            var buttonNames = this.Arguments.GetStringArray(Constants.BUNDLE_MAIL_REPLY_BUTTONS);
            if (buttonNames.Length == 0)
                buttonNames = new[] { "OK" };
            foreach (var buttonName in buttonNames)
            {
                var button = new Button(view.Context)
                {
                    Text = buttonName
                };
                button.Click += Button_Click;
                buttonsLayout.AddView(button);
            }

            return view;
        }

        public override void OnResume()
        {
            base.OnResume();
            var pm = (PowerManager)Activity.GetSystemService(Android.Content.Context.PowerService);
            wakeLock = pm.NewWakeLock(WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease, "BananaMailBoard");
            wakeLock.Acquire();
            if (!soundStop)
            {
                PlayMessageAlertTone();
            }
        }

        public override void OnPause()
        {
            base.OnPause();
            StopMessageAlertTone();
            if (wakeLock != null)
            {
                wakeLock.Release();
                wakeLock = null;
            }
        }

        private void PlayMessageAlertTone()
        {
            spMessageAlertTone = new SoundPool(1, Stream.Music, 0);
            soundId = spMessageAlertTone.Load(this.Activity, Resource.Raw.sound, 1);
            spMessageAlertTone.LoadComplete += (sender, e) =>
            {
                if (spMessageAlertTone != null) spMessageAlertTone.Play(soundId, 1, 1, 1, -1, 1);
            };
        }

        private void StopMessageAlertTone()
        {
            if (spMessageAlertTone != null)
            {
                spMessageAlertTone.Stop(soundId);
                spMessageAlertTone.Unload(soundId);
                spMessageAlertTone.Dispose();
                spMessageAlertTone = null;
            }
        }

        private async void Button_Click(object sender, EventArgs e)
        {
            // 返信中プログレスダイアログ表示
            var progressDialog = new ProgressDialog(Activity);
            progressDialog.SetTitle("返信メール送信中");
            progressDialog.SetMessage("しばらくお待ちください。");
            progressDialog.SetCancelable(false);
            progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            progressDialog.Show();

            // 返信
            var replyResult = await SendReply(((Button)sender).Text);

            // 返信中プログレスダイアログ閉じる
            progressDialog.Dismiss();

            if (replyResult)
            {
                // 返信に成功した場合は待受け画面へ遷移
                this.NavigateFragment(new MailReceiveWaitingFragment());
            }
            else
            {
                // 返信に失敗した場合はエラーダイアログ表示
                var alertDlgBuilder = new AlertDialog.Builder(this.Activity);
                alertDlgBuilder.SetTitle("返信送信エラー");
                alertDlgBuilder.SetMessage("返信送信中にエラーが発生しました。");
                alertDlgBuilder.SetPositiveButton("OK", (sender2, e2) => { });
                alertDlgBuilder.Create().Show();
            }
        }

        private Task<bool> SendReply(string replyMessage)
        {
            // メール設定取得
            var pref = PreferenceManager.GetDefaultSharedPreferences(this.Activity);
            var mailAddress = pref.GetString("mail_address", "");
            var mailPassword = pref.GetString("mail_password", "");
            var smtpServerAddress = pref.GetString("mail_smtp_server", "");
            var smtpUseAuth = pref.GetBoolean("mail_smtp_useauth", false);
            var smtpUseSsl = pref.GetBoolean("mail_smtp_usessl", false);
            var strSmtpPort = pref.GetString("mail_smtp_port", "");
            int smtpPort;
            if (string.IsNullOrWhiteSpace(strSmtpPort) || !int.TryParse(strSmtpPort, out smtpPort))
            {
                smtpPort = smtpUseAuth ? 587 : smtpUseSsl ? 465 : 25;
            }
            // 返信内容取得
            var replyAddress = Arguments.GetString(Constants.BUNDLE_MAIL_FROM_ADDRESS);
            var mailSubject = Arguments.GetString(Constants.BUNDLE_MAIL_SUBJECT);
            var mailBody = Arguments.GetString(Constants.BUNDLE_MAIL_BODY);
            var messageUid = Arguments.GetString(Constants.BUNDLE_MAIL_MESSAGE_UID);

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    // 返信メール送信
                    var fromAddress = new MailAddress(mailAddress);
                    var toAddress = new MailAddress(replyAddress);
                    using (var message = new MailMessage(fromAddress, toAddress))
                    {
                        message.Subject = "Re:" + mailSubject;
                        message.SubjectEncoding = System.Text.Encoding.UTF8;
                        message.Body =
                            $"「{replyMessage}」ボタンにより返信されました。\r\n\r\n" +
                            Regex.Replace(mailBody, "^", "> ", RegexOptions.Multiline);
                        message.BodyEncoding = System.Text.Encoding.UTF8;
                        using (var smtpClient = new SmtpClient())
                        {
                            smtpClient.Host = smtpServerAddress;
                            smtpClient.Port = smtpPort;
                            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
                            if (smtpUseSsl)
                            {
                                smtpClient.EnableSsl = true;
                            }
                            if (smtpUseAuth)
                            {
                                smtpClient.Credentials = new NetworkCredential(mailAddress, mailPassword);
                            }
                            smtpClient.Send(message);
                        }
                    }

                    // 送信済みメール
                    lock (Constants.DB_LOCK)
                    {
                        using (var db = new SQLiteConnection(Constants.DB_PATH))
                        {
                            db.CreateTable<RepliedMessage>();
                            db.InsertOrReplace(new RepliedMessage() { MessageUid = messageUid });
                        }
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    Log.Error(Constants.LOG_TAG, "返信エラー" + ex.ToString());
                }
                return false;
            });
        }
    }
}