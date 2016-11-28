using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

namespace Phoneword.Droid
{
    [Activity(Label = "Phoneword", Icon = "@drawable/icon", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation)]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsApplicationActivity
    {
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            global::Xamarin.Forms.Forms.Init(this, bundle);

			Xamarin.Forms.Forms.ViewInitialized += (object sender, Xamarin.Forms.ViewInitializedEventArgs e) => {
				if (!string.IsNullOrWhiteSpace(e.View.AutomationId))
				{
					//Console.WriteLine("Set ID of '{0}'", e.View.AutomationId);
					e.NativeView.ContentDescription = e.View.AutomationId;
				}
			};

            LoadApplication(new App());
        }
    }
}

