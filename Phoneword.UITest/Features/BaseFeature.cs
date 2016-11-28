using System;
using TechTalk.SpecFlow;
using Xamarin.UITest;
using Xamarin.UITest.Configuration;

namespace Phoneword.UITest
{
	[Binding]
	public abstract class BaseFeature
	{
		public const string DEVICE_DESCRIPTION = "DeviceDescription";
		public const AppDataMode DEFAULT_DATA_MODE = AppDataMode.DoNotClear;
		//public static IApp AppUnderTest;
		public static BaseFeature CurrentFeature;

		public readonly Platform TestPlatform;
		public readonly string DeviceDescription;
		public readonly AppDataMode TestMode;

		protected BaseFeature(Platform platform, string device=null, AppDataMode mode=DEFAULT_DATA_MODE)
		{
			TestPlatform = platform;
			DeviceDescription = device;
			TestMode = mode;

			CurrentFeature = this;
		}

        /// <summary>
        /// Load appropriate screens for different platform, thus to be accesseed by Steps.
        /// For complex apps with multiple screens, Reflection with customised attributes could be used.
        /// </summary>
        public abstract void SetScreens();

		public override string ToString()
		{
			return string.Format(@"{0}: {1} device({2}){3}", this.GetType().Name, TestPlatform, DeviceDescription
				, TestMode==DEFAULT_DATA_MODE?"" : string.Format(" of {0} mode.", TestMode));
		}

		[BeforeFeature]
		public static void BeforeFeature()
		{

			Platform platform = CurrentFeature.TestPlatform;
			string device = CurrentFeature.DeviceDescription;
			AppDataMode mode = CurrentFeature.TestMode;

			FeatureContext.Current.Set<Platform>(platform);
			FeatureContext.Current.Set<string>(device, DEVICE_DESCRIPTION);
			FeatureContext.Current.Set<AppDataMode>(mode);

			CurrentFeature.SetScreens();

			//IApp app = Extensions.StartApp(platform, device, mode);
			IApp app = Extensions.StartApp(platform, device, mode);
			FeatureContext.Current.Set<IApp>(app);
		}
	}
}
