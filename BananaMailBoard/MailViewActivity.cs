using System;
using System.Linq;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Net.Mail;
using System.Net;
using System.Timers;

using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Media;
using Android.App;
using Android.Preferences;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;

using SQLite;

namespace BananaMailBoard
{
    using Entity;
    using Util;

    [Activity(LaunchMode = LaunchMode.SingleInstance, ScreenOrientation = ScreenOrientation.Landscape, 
        ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MailViewActivity : Activity
    {
        private bool ringing = false;
        private bool flickOn = false;

        private MediaPlayer mpRingingTone = null;
        private Timer flickerTimer = null;

        static MailViewActivity()
        {
            ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, errors) =>
            {
                return true;
            };
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.MailView);
            OnNewIntent(Intent);
        }

        public override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();
            Window.AddFlags(WindowManagerFlags.TurnScreenOn | WindowManagerFlags.KeepScreenOn | WindowManagerFlags.ShowWhenLocked | WindowManagerFlags.DismissKeyguard);
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            Intent = intent;

            // 前回のボタンをクリア
            var buttonsLayout = FindViewById<LinearLayout>(Resource.Id.buttonsLayout);
            buttonsLayout.RemoveAllViews();

            // 差出人設定
            var fromName = intent.GetStringExtra(Constants.BUNDLE_MAIL_FROM_NAME);
            if (!string.IsNullOrWhiteSpace(fromName))
            {
                FindViewById<TextView>(Resource.Id.txtFromName).Text = fromName;
            }
            else
            {
                FindViewById<TextView>(Resource.Id.txtFromName).Text = intent.GetStringExtra(Constants.BUNDLE_MAIL_FROM_ADDRESS);
            }
            // 件名設定
            FindViewById<TextView>(Resource.Id.txtSubject).Text = intent.GetStringExtra(Constants.BUNDLE_MAIL_SUBJECT);
            // 本文設定
            FindViewById<TextView>(Resource.Id.txtBody).Text = intent.GetStringExtra(Constants.BUNDLE_MAIL_BODY);
            // 返信ボタン
            var buttonNames = intent.GetStringArrayExtra(Constants.BUNDLE_MAIL_REPLY_BUTTONS);
            foreach (var button in buttonNames
                .DefaultIfEmpty("OK")
                .Select(btnName => new Button(this) { Text = btnName }))
            {
                button.SetTextSize(Android.Util.ComplexUnitType.Sp, 40);
                button.Click += ReplyButton_Click;
                buttonsLayout.AddView(button);
            }
            ((MainApplication)Application).LastViewMessageUid = intent.GetStringExtra(Constants.BUNDLE_MAIL_MESSAGE_UID);
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (!ringing)
            {
                StartMessageReciveRinging();
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            StopMessageReceiveRinging();
        }

        /// <summary>
        /// 返信ボタンクリック時処理。
        /// </summary>
        private async void ReplyButton_Click(object sender, EventArgs e)
        {
            try
            {
                // 鳴動停止
                StopMessageReceiveRinging();

                // 返信中プログレスダイアログ表示
                var progressDialog = new ProgressDialog(this);
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
                    Finish();
                    StartActivity(typeof(MainActivity));
                }
                else
                {
                    // 返信に失敗した場合はエラーダイアログ表示
                    var alertDlgBuilder = new AlertDialog.Builder(this);
                    alertDlgBuilder.SetTitle("返信送信エラー");
                    alertDlgBuilder.SetMessage("返信送信中にエラーが発生しました。");
                    alertDlgBuilder.SetPositiveButton("OK", (sender2, e2) => { });
                    alertDlgBuilder.Create().Show();
                }

            }
            catch (Exception ex)
            {
                LogHelper.Error(ex, "Reply Button Click Error");
            }
        }

        /// <summary>
        /// メール受信鳴動音とフリッカを開始する。
        /// </summary>
        private void StartMessageReciveRinging()
        {
            if (mpRingingTone == null)
            {
                mpRingingTone = MediaPlayer.Create(this, Resource.Raw.sound);
                mpRingingTone.Looping = true;
                mpRingingTone.Start();
            }

            if (flickerTimer == null)
            {
                flickerTimer = new Timer(Constants.FLICKER_INTERVAL);
                flickerTimer.Elapsed += (sender, e) => RunOnUiThread(() =>
                {
                    flickOn = !flickOn;
                    SetFlickerStyle(flickOn);
                });
                flickerTimer.Start();
            }
        }

        /// <summary>
        /// メール受信鳴動音とフリッカを停止する。
        /// </summary>
        private void StopMessageReceiveRinging()
        {
            if (mpRingingTone != null)
            {
                mpRingingTone.Stop();
                mpRingingTone.Dispose();
                mpRingingTone = null;
            }
            if (flickerTimer != null)
            {
                flickerTimer.Stop();
                flickerTimer.Dispose();
                flickerTimer = null;
                flickOn = false;
                SetFlickerStyle(false);
            }
        }

        /// <summary>
        /// フリッカスタイルをセットします。
        /// </summary>
        /// <param name="onOff"></param>
        private void SetFlickerStyle(bool onOff)
        {
            var rootLayout = FindViewById<LinearLayout>(Resource.Id.rootLayout);
            if (flickOn)
            {
                rootLayout.SetBackgroundColor(Constants.FLICKER_COLOR);
            }
            else
            {
                var attrValue = new Android.Util.TypedValue();
                Theme.ResolveAttribute(Android.Resource.Attribute.ColorBackground, attrValue, true);
                rootLayout.SetBackgroundColor(Resources.GetColor(attrValue.ResourceId));
            }
        }

        /// <summary>
        /// メール返信処理。
        /// </summary>
        /// <param name="replyButtonName">返信ボタン名称。</param>
        /// <returns>送信成功時はtrue、失敗時はfalse。</returns>
        private Task<bool> SendReply(string replyButtonName)
        {
            // メール設定取得
            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
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
            var intent = this.Intent;
            var replyAddress = intent.GetStringExtra(Constants.BUNDLE_MAIL_FROM_ADDRESS);
            var mailSubject = intent.GetStringExtra(Constants.BUNDLE_MAIL_SUBJECT);
            var mailBody = intent.GetStringExtra(Constants.BUNDLE_MAIL_BODY);
            var messageUid = intent.GetStringExtra(Constants.BUNDLE_MAIL_MESSAGE_UID);

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
                            $"「{replyButtonName}」ボタンにより返信されました。\r\n\r\n" +
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
                    LogHelper.Error(ex, "Send Reply Error");
                }
                return false;
            });
        }
    }
}