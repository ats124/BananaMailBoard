using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Preferences;

namespace BananaMailBoard
{
    public abstract class BasePreferenceFragment : PreferenceFragment, IBaseFragment
    {
        public BasePreferenceFragment()
        {
            this.RetainInstance = true;
        }

        protected MainActivity MainActivity
        {
            get { return this.Activity as MainActivity; }
        }

        public override void OnResume()
        {
            base.OnResume();

            var mainActivity = this.MainActivity;
            if (mainActivity != null)
            {
                mainActivity.CurrentFragment = this;
                OnResume(mainActivity.PendingIntents.ToArray());
                mainActivity.PendingIntents.Clear();
            }
        }

        protected virtual void OnResume(Intent[] intents)
        {
        }

        public virtual void OnNewIntent(Intent intent)
        {
        }

        public virtual bool OnTouchEvent(MotionEvent e)
        {
            return false;
        }

        public override void OnPause()
        {
            base.OnPause();

            var mainActivity = this.MainActivity;
            if (mainActivity != null)
            {
                if (mainActivity.CurrentFragment == this)
                {
                    mainActivity.CurrentFragment = null;
                }
            }
        }

        protected void NavigateFragment(IBaseFragment nextFragment, bool addToBackStack = false)
        {
            var fragmentTran = FragmentManager.BeginTransaction();
            fragmentTran.Replace(Resource.Id.frameLayout, (Fragment)nextFragment);
            if (addToBackStack)
                fragmentTran.AddToBackStack(null);
            fragmentTran.Commit();

            var mainActivity = this.MainActivity;
            if (mainActivity != null)
            {
                mainActivity.CurrentFragment = null;
            }
        }
    }
}