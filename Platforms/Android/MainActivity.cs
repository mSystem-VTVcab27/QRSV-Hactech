using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Nfc;
using Android.OS;
using System;

namespace MauiApp1tesst
{
    [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        private NfcAdapter? _nfcAdapter;
        public static event Action<string>? OnNfcCardScanned;

        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            _nfcAdapter = NfcAdapter.GetDefaultAdapter(this);
        }

        protected override void OnResume()
        {
            base.OnResume();
            if (_nfcAdapter != null)
            {
                var intent = new Intent(this, typeof(MainActivity)).AddFlags(ActivityFlags.SingleTop);
                var pendingIntentFlags = Build.VERSION.SdkInt >= BuildVersionCodes.S 
                    ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable 
                    : PendingIntentFlags.UpdateCurrent;
                var pendingIntent = PendingIntent.GetActivity(this, 0, intent, pendingIntentFlags);
                
                _nfcAdapter.EnableForegroundDispatch(this, pendingIntent, null, null);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            _nfcAdapter?.DisableForegroundDispatch(this);
        }

        protected override void OnNewIntent(Intent? intent)
        {
            base.OnNewIntent(intent);
            if (intent != null && (NfcAdapter.ActionTagDiscovered.Equals(intent.Action) || 
                                   NfcAdapter.ActionTechDiscovered.Equals(intent.Action) || 
                                   NfcAdapter.ActionNdefDiscovered.Equals(intent.Action)))
            {
                var tag = (Tag?)intent.GetParcelableExtra(NfcAdapter.ExtraTag);
                if (tag != null)
                {
                    byte[] idBytes = tag.GetId();
                    string cardUid = BitConverter.ToString(idBytes).Replace("-", "").ToUpper();
                    
                    // Trigger the static event
                    OnNfcCardScanned?.Invoke(cardUid);
                }
            }
        }
    }
}
