using System;
using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace BananaMailBoard
{
    [Activity(Label = "BananaMailBoard", MainLauncher = true, Icon = "@drawable/icon", LaunchMode = LaunchMode.SingleInstance, 
        ScreenOrientation = ScreenOrientation.Landscape, ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.KeyboardHidden | ConfigChanges.ScreenSize)]
    public class MainActivity : Activity
    {
        const int MENU_ID_PREFERENCE = 0;

        /// <summary>
        /// 現在表示中のフラグメント
        /// </summary>
        internal IBaseFragment CurrentFragment { get; set; }

        /// <summary>
        /// 表示中フラグメントが定まっていない間に受信したインテント。
        /// </summary>
        internal Queue<Intent> PendingIntents { get; } = new Queue<Intent>();

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.Main);

            // インテント受信による起動
            if (this.Intent != null)
            {
                this.PendingIntents.Enqueue(this.Intent);
            }

            if (savedInstanceState == null)
            {
                var fragmentTran = FragmentManager.BeginTransaction();
                fragmentTran.Replace(Resource.Id.frameLayout, new MailReceiveWaitingFragment());
                fragmentTran.Commit();
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var configMenuItem = menu.Add(Menu.None, MENU_ID_PREFERENCE, Menu.None, "設定");
            configMenuItem.SetIcon(Android.Resource.Drawable.IcMenuPreferences);
            configMenuItem.SetShowAsAction(ShowAsAction.Always | ShowAsAction.WithText);
            return true;
        }

        public override bool OnMenuItemSelected(int featureId, IMenuItem item)
        {
            switch (featureId)
            {
                case MENU_ID_PREFERENCE:
                    if (!(CurrentFragment is MailPreferenceFragment))
                    {
                        var fragmentTran = FragmentManager.BeginTransaction();
                        fragmentTran.Replace(Resource.Id.frameLayout, new MailPreferenceFragment());
                        fragmentTran.AddToBackStack(null);
                        fragmentTran.Commit();
                        CurrentFragment = null;
                    }
                    return true;
                default:
                    return base.OnMenuItemSelected(featureId, item);
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);

            if (this.CurrentFragment != null)
            {
                this.CurrentFragment.OnNewIntent(intent);
            }
            else
            {
                this.PendingIntents.Enqueue(intent);
            }
        }

        public override bool OnTouchEvent(MotionEvent e)
        {
            if (CurrentFragment != null)
            {
                base.OnTouchEvent(e);
                return CurrentFragment.OnTouchEvent(e);
            }
            else
            {
                return base.OnTouchEvent(e);
            }
        }
    }
}

