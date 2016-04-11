using System;
using System.Linq;
using System.Text.RegularExpressions;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Net;
using Android.Preferences;

using MailKit.Net.Pop3;
using MimeKit;

namespace BananaMailBoard
{
    using Util;

    [Service]
    public class MailReceiveService : IntentService
    {
        static string lastMessageUid = null;
        static Bundle lastBundleMessage = null;

        /// <summary>
        /// 文末に「」で囲まれた文字列があれば返信ボタンとして認識するための正規表現
        /// </summary>
        static readonly Regex BUTTONS_REGEX = new Regex(@"((\n|\s)*「(?<button>[^」*])」(\n|\s)*)$");

        protected override void OnHandleIntent(Intent intent)
        {
            Log.Debug(Constants.LOG_TAG, "MailReceiveService.OnHandleIntent");

            // メール受信
            MimeMessage message;
            string messageUid;
            messageUid = GetMessage(lastMessageUid, out message);

            Bundle bundleMessage = null;
            if (messageUid != null)
            {
                try
                {
                    if (messageUid == lastMessageUid)
                    {
                        // 前回メッセージのUIDが一致した場合は前回のBundleを参照
                        bundleMessage = lastBundleMessage;
                    }
                    else
                    {
                        bundleMessage = CreateBundleMessage(message);
                    }

                    var toIntent = new Intent(BaseContext, typeof(MainActivity));
                    toIntent.SetFlags(ActivityFlags.NewTask);
                    toIntent.AddFlags(ActivityFlags.SingleTop);
                    toIntent.SetAction(Constants.INTENT_MAIL_RECEIVE_ACTION);
                    toIntent.PutExtras(bundleMessage);
                    StartActivity(toIntent);
                }
                catch (Exception ex)
                {
                    Log.Error(Constants.LOG_TAG, ex.ToString());
                }
            }

            lastMessageUid = messageUid;
            lastBundleMessage = bundleMessage;
        }

        private string GetMessage(string lastMessageUid, out MimeMessage message)
        {
            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            var mailAddress = pref.GetString("mail_address", "");
            var mailPassword = pref.GetString("mail_password", "");
            var serverAddress = pref.GetString("mail_pop3_server", "");
            var useSsl = pref.GetBoolean("mail_pop3_usessl", false);
            var strPort = pref.GetString("mail_pop3_port", "");
            int port;
            if (string.IsNullOrWhiteSpace(strPort) || int.TryParse(strPort, out port))
            {
                port = useSsl ? 995 : 110;
            }

            if (string.IsNullOrWhiteSpace(mailAddress) || string.IsNullOrWhiteSpace(serverAddress))
            {
                message = null;
                return null;
            }

            var cm = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
            if (cm.ActiveNetworkInfo?.IsConnected ?? false)
            {
                using (var pop3Client = new Pop3Client())
                {
                    try
                    {
                        pop3Client.Connect(serverAddress, port, true);
                        pop3Client.Authenticate(mailAddress, mailPassword);
                        var messageCnt = pop3Client.Count;
                        if (messageCnt > 0)
                        {
                            var messageUid = pop3Client.GetMessageUid(0);
                            if (messageUid != lastMessageUid)
                            {
                                Log.Debug(Constants.LOG_TAG, "New Mail Found");
                                message = pop3Client.GetMessage(0);
                            }
                            else
                            {
                                // 前回と同じUIDの場合メール本文を取得しない
                                message = null;
                            }
                            return messageUid;
                        }
                        else
                        {
                            Log.Debug(Constants.LOG_TAG, "No Mail");
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(Constants.LOG_TAG, "POP3 Error:" + ex.ToString());
                    }
                    finally
                    {
                        try
                        {
                            pop3Client.Disconnect(true);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
            }
            else
            {
                Log.Debug(Constants.LOG_TAG, "No Network");
            }
            message = null;
            return null;
        }

        private Bundle CreateBundleMessage(MimeMessage message)
        {
            string textBody;
            if (message.TextBody != null)
            {
                textBody = message.TextBody;
            }
            else if (message.HtmlBody != null)
            {
                // HTML形式の場合はテキストに変換する
                textBody = HtmlConverter.HtmlToText(message.HtmlBody);
            }
            else
            {
                textBody = "";
            }

            // 文末の返信ボタン用文字列を抽出
            var m = BUTTONS_REGEX.Match(textBody);
            string[] buttons;
            if (m.Success)
            {
                buttons = m.Captures.Cast<Capture>().Select(c => c.Value).ToArray();
                // 返信用ボタン文字列を本文から除去
                textBody = textBody.Substring(0, m.Index - 1);
            }
            else
            {
                buttons = new string[0];
            }

            var bundleMessage = new Bundle();
            bundleMessage.PutString(Constants.BUNDLE_MAIL_SUBJECT, message.Subject ?? "");
            bundleMessage.PutString(Constants.BUNDLE_MAIL_BODY, textBody);
            bundleMessage.PutString(Constants.BUNDLE_MAIL_FROM_ADDRESS, ((MailboxAddress)message.From[0]).Address);
            bundleMessage.PutStringArray(Constants.BUNDLE_MAIL_REPLY_BUTTONS, buttons);

            return bundleMessage;
        }
    }
}

