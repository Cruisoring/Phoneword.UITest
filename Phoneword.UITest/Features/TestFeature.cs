using System;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Configuration;
using TechTalk.SpecFlow;

namespace Phoneword.UITest.Features
{
	public partial class TestFeature : BaseFeature
    {
        public TestFeature(Platform platform, string device = null, AppDataMode mode = DEFAULT_DATA_MODE)
            : base(platform, device, mode)
        { }



        public override void SetScreens()
        {
			/*/
			FeatureContext.Current.Set<IPhonewordScreen>(new AndroidPhonewordScreen());
			/*/
			if (TestPlatform == Platform.iOS)
            {
                FeatureContext.Current.Set<IPhonewordScreen>(new PhonewordScreen());
            }
            else
            {
                FeatureContext.Current.Set<IPhonewordScreen>(new AndroidPhonewordScreen());
            }
            //*/
        }
    }

    [TestFixture]
    [Category("Cloud")]
    [Category("Android")]
    public class Android_TestFeature : TestFeature
    {
        public Android_TestFeature()
            : base(Platform.Android, null)
        { }
    }

    [TestFixture]
	[Category("Local")]
	[Category("Android")]
	[Category("S6")]
	public class S6_TestFeature : TestFeature
	{
		public S6_TestFeature()
			: base(Platform.Android, "23, SM-G925")
		{ }
	}

	[TestFixture]
	[Category("Local")]
	[Category("Android")]
	[Category("I9300")]
	public class I9300_TestFeature : TestFeature
	{
		public I9300_TestFeature()
			: base(Platform.Android, "4.1.2,API16,GT-I9300")
		{ }
	}


	[TestFixture]
    [Category("Cloud")]
    [Category("iPhone")]
    public class iPhone_TestFeature : TestFeature
    {
        public iPhone_TestFeature()
            : base(Platform.iOS, "iP")
        { }
    }

	[TestFixture]
	[Category("Cloud")]
	[Category("iPhone")]
	[Category("Simulator")]
	public class iPhoneSimulator_TestFeature : TestFeature
	{
		public iPhoneSimulator_TestFeature()
			: base(Platform.iOS, "iPhone 6 Plus, Simulator")
		{ }
	}

}
