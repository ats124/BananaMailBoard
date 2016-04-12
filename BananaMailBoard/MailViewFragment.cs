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
            // �ԐM���v���O���X�_�C�A���O�\��
            var progressDialog = new ProgressDialog(Activity);
            progressDialog.SetTitle("�ԐM���[�����M��");
            progressDialog.SetMessage("���΂炭���҂����������B");
            progressDialog.SetCancelable(false);
            progressDialog.SetProgressStyle(ProgressDialogStyle.Spinner);
            progressDialog.Show();

            // �ԐM
            var replyResult = await SendReply(((Button)sender).Text);

            // �ԐM���v���O���X�_�C�A���O����
            progressDialog.Dismiss();

            if (replyResult)
            {
                // �ԐM�ɐ��������ꍇ�͑Ҏ󂯉�ʂ֑J��
                this.NavigateFragment(new MailReceiveWaitingFragment());
            }
            else
            {
                // �ԐM�Ɏ��s�����ꍇ�̓G���[�_�C�A���O�\��
                var alertDlgBuilder = new AlertDialog.Builder(this.Activity);
                alertDlgBuilder.SetTitle("�ԐM���M�G���[");
                alertDlgBuilder.SetMessage("�ԐM���M���ɃG���[���������܂����B");
                alertDlgBuilder.SetPositiveButton("OK", (sender2, e2) => { });
                alertDlgBuilder.Create().Show();
            }
        }

        private Task<bool> SendReply(string replyMessage)
        {
            // ���[���ݒ�擾
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
            // �ԐM���e�擾
            var replyAddress = Arguments.GetString(Constants.BUNDLE_MAIL_FROM_ADDRESS);
            var mailSubject = Arguments.GetString(Constants.BUNDLE_MAIL_SUBJECT);
            var mailBody = Arguments.GetString(Constants.BUNDLE_MAIL_BODY);
            var messageUid = Arguments.GetString(Constants.BUNDLE_MAIL_MESSAGE_UID);

            return Task.Factory.StartNew(() =>
            {
                try
                {
                    // �ԐM���[�����M
                    var fromAddress = new MailAddress(mailAddress);
                    var toAddress = new MailAddress(replyAddress);
                    using (var message = new MailMessage(fromAddress, toAddress))
                    {
                        message.Subject = "Re:" + mailSubject;
                        message.SubjectEncoding = System.Text.Encoding.UTF8;
                        message.Body =
                            $"�u{replyMessage}�v�{�^���ɂ��ԐM����܂����B\r\n\r\n" +
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

                    // ���M�ς݃��[��
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
                    Log.Error(Constants.LOG_TAG, "�ԐM�G���[" + ex.ToString());
                }
                return false;
            });
        }
    }
}