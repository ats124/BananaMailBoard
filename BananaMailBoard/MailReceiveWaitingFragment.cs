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

namespace BananaMailBoard
{
    public class MailReceiveWaitingFragment : BaseFragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MailReceiveWaiting, container, false);            
            return view;
        }

        public override void OnStart()
        {
            base.OnStart();
            this.Activity.StartService(new Intent(this.Activity, typeof(MailReceiveService)));
        }

        protected override void OnResume(Intent[] intents)
        {
            base.OnResume(intents);
            ProceedIntent(intents);
        }

        public override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            ProceedIntent(new[] { intent });
        }

        private void ProceedIntent(IEnumerable<Intent> intents)
        {
            var mailReceiveIntent = intents.FirstOrDefault(i => i.Action == Constants.INTENT_MAIL_RECEIVE_ACTION);
            if (mailReceiveIntent == null)
            {
                return;
            }

            var mailViewFragment = new MailViewFragment();
            mailViewFragment.Arguments = new Bundle(mailReceiveIntent.Extras);
            NavigateFragment(mailViewFragment);
        }
    }
}