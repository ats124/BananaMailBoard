using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BananaMailBoard
{
    class Constants
    {
        public const string LOG_TAG = "BananaMailBoard";
        public const string DB_FILE_NAME = "BananaMailBoard.db3";
        public const string INTENT_MAIL_RECEIVE_ACTION = "jp.atsnw.BananaMailBoard.MAIL_RECEIVE";

        public const string BUNDLE_MAIL_MESSAGE_UID = "MAIL_MESSAGE_UID";
        public const string BUNDLE_MAIL_BODY = "MAIL_BODY";
        public const string BUNDLE_MAIL_SUBJECT = "MAIL_SUBJECT";
        public const string BUNDLE_MAIL_FROM_ADDRESS = "MAIL_FROM_ADDRESS";
        public const string BUNDLE_MAIL_REPLY_BUTTONS = "MAIL_REPLY_BUTTONS";

        public static readonly object DB_LOCK = new object(); 

        public static string DB_PATH
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DB_FILE_NAME); }
        }
    }
}