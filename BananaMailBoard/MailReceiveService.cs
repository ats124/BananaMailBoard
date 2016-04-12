using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Android.Net;
using Android.Preferences;

using SQLite;
using MailKit.Net.Pop3;
using MimeKit;

namespace BananaMailBoard
{
    using Entity;
    using Util;

    /// <summary>
    /// メール受信サービス
    /// </summary>
    [Service]
    public class MailReceiveService : IntentService
    {
        static string lastMessageUid = null;
        static Bundle lastBundleMessage = null;

        /// <summary>
        /// 文末に「」で囲まれた文字列があれば返信ボタンとして認識するための正規表現
        /// </summary>
        static readonly Regex BUTTONS_REGEX = new Regex(@"((\n|\s)*(「(?<button>.*?)」\s*)*(\n|\s)*)$");

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
                        bundleMessage = CreateBundleMessage(messageUid, message);
                    }

                    var toIntent = new Intent(BaseContext, typeof(MainActivity));
                    toIntent.SetFlags(ActivityFlags.NewTask);
                    toIntent.AddFlags(ActivityFlags.SingleTop);
                    toIntent.SetAction(Constants.INTENT_MAIL_RECEIVE_ACTION);
                    toIntent.PutExtras(bundleMessage);
                    Log.Debug(Constants.LOG_TAG, "MailReceiveService.OnHandleIntent StartActivity Start");
                    StartActivity(toIntent);
                    Log.Debug(Constants.LOG_TAG, "MailReceiveService.OnHandleIntent StartActivity End");

                    lastMessageUid = messageUid;
                    lastBundleMessage = bundleMessage;
                }
                catch (Exception ex)
                {
                    Log.Error(Constants.LOG_TAG, ex.ToString());
                }
            }
        }

        private string GetMessage(string lastMessageUid, out MimeMessage message)
        {
            // メール設定取得
            var pref = PreferenceManager.GetDefaultSharedPreferences(this);
            var mailAddress = pref.GetString("mail_address", "");
            var mailPassword = pref.GetString("mail_password", "");
            var serverAddress = pref.GetString("mail_pop3_server", "");
            var useSsl = pref.GetBoolean("mail_pop3_usessl", false);
            var strPort = pref.GetString("mail_pop3_port", "");
            int port;
            if (string.IsNullOrWhiteSpace(strPort) || !int.TryParse(strPort, out port))
            {
                port = useSsl ? 995 : 110;
            }
            // メールアドレス、サーバーアドレスの設定がなければ処理中断
            if (string.IsNullOrWhiteSpace(mailAddress) || string.IsNullOrWhiteSpace(serverAddress))
            {
                message = null;
                return null;
            }

            // 返信済みメッセージID一覧取得
            HashSet<string> repliedMessageIds;
            lock (Constants.DB_LOCK)
            {
                using (var db = new SQLiteConnection(Constants.DB_PATH))
                {
                    db.CreateTable<RepliedMessage>();
                    repliedMessageIds = new HashSet<string>(db.Table<RepliedMessage>().ToList().Select(rec => rec.MessageUid));                    
                }
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

                        // メッセージID一覧を取得
                        var messageUids = pop3Client.GetMessageUids();

                        // メッセージIDの中から返信していない最初のメッセージIDのインデックスを取得
                        var newMessageInfo = messageUids
                            .Select((uid, index) => new { uid, index })
                            .FirstOrDefault(obj => !repliedMessageIds.Contains(obj.uid));

                        if (newMessageInfo != null)
                        {
                            if (newMessageInfo.uid != lastMessageUid)
                            {
                                Log.Debug(Constants.LOG_TAG, "New Mail Found");
                                message = pop3Client.GetMessage(newMessageInfo.index);
                            }
                            else
                            {
                                // 前回と同じUIDの場合メール本文を取得しない
                                message = null;
                            }
                            return newMessageInfo.uid;
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

        /// <summary>
        /// インテント送信用メッセージバンドルを生成。
        /// </summary>
        /// <param name="messageUid">バンドルメッセージ作成元となるメッセージUID。</param>
        /// <param name="message">バンドルメッセージ作成元となる<see cref="MimeMessage"/></param>
        /// <returns>引数のmessageをもとに作成したバンドル。</returns>
        private Bundle CreateBundleMessage(string messageUid, MimeMessage message)
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
                buttons = m.Groups["button"].Captures.Cast<Capture>().Select(c => c.Value).ToArray();
                // 返信用ボタン文字列を本文から除去
                textBody = textBody.Substring(0, m.Index);
            }
            else
            {
                buttons = new string[0];
            }

            var bundleMessage = new Bundle();
            bundleMessage.PutString(Constants.BUNDLE_MAIL_MESSAGE_UID, messageUid);
            bundleMessage.PutString(Constants.BUNDLE_MAIL_SUBJECT, message.Subject ?? "");
            bundleMessage.PutString(Constants.BUNDLE_MAIL_BODY, textBody);
            bundleMessage.PutString(Constants.BUNDLE_MAIL_FROM_ADDRESS, ((MailboxAddress)message.From[0]).Address);
            bundleMessage.PutStringArray(Constants.BUNDLE_MAIL_REPLY_BUTTONS, buttons);

            return bundleMessage;
        }
    }
}

