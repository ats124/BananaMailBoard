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

namespace BananaMailBoard
{
    class Constants
    {
        public const string LOG_TAG = "BananaMailBoard";
        public const string INTENT_MAIL_RECEIVE_ACTION = "jp.atsnw.BananaMailBoard.MAIL_RECEIVE";

        public const string BUNDLE_MAIL_BODY = "MAIL_BODY";
        public const string BUNDLE_MAIL_SUBJECT = "MAIL_SUBJECT";
        public const string BUNDLE_MAIL_FROM_ADDRESS = "MAIL_FROM_ADDRESS";
        public const string BUNDLE_MAIL_REPLY_BUTTONS = "MAIL_REPLY_BUTTONS";
    }
}