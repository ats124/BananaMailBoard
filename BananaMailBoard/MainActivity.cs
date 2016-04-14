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

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.Main);
            ((MainApplication)Application).CurrentMainActivity = this;
            AlarmReceiver.SetDoMailReceiveAlarm(this, true);
        }

        protected override void OnStart()
        {
            base.OnStart();
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            var configMenuItem = menu.Add(Menu.None, MENU_ID_PREFERENCE, Menu.None, "設定");
            configMenuItem.SetIcon(Android.Resource.Drawable.IcMenuPreferences);
            configMenuItem.SetShowAsAction(ShowAsAction.Always | ShowAsAction.WithText);
            return true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            var mainApp = (MainApplication)Application;
            if (mainApp.CurrentMainActivity == this)
            {
                mainApp.CurrentMainActivity = null;
            }
        }

        public override bool OnMenuItemSelected(int featureId, IMenuItem item)
        {
            switch (featureId)
            {
                case MENU_ID_PREFERENCE:
                    StartActivity(typeof(MailPreferenceActivity));
                    return true;
                default:
                    return base.OnMenuItemSelected(featureId, item);
            }
        }
    }
}

