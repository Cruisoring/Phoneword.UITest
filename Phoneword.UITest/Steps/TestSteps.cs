using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using TechTalk.SpecFlow;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Phoneword.UITest.Steps
{
    [Binding]
    public sealed class TestSteps
    {
        private IApp app = FeatureContext.Current.Get<IApp>();
        private IPhonewordScreen screen = FeatureContext.Current.Get<IPhonewordScreen>();

		/// <summary>
		/// A sample AppQuery generator to locate element with case incensitive text.
		/// </summary>
		/// <returns>AppQuery to locate the targeted button.</returns>
		/// <param name="text">The text to be matched against text(Android) or label(iOS).</param>
		public static Func<AppQuery, AppQuery> InsensitiveText(string text)
		{
			//Cannot get the button by text ignoring the case, and not working with iPhone!
			//return x => x.Text(text);

			IApp a = FeatureContext.Current.Get<IApp>();
			if (string.IsNullOrEmpty(text))
				throw new ArgumentNullException();;
			var all = a.Query(x => x.All());
			var matched = all.FirstOrDefault(x => String.Equals(text,  x.Text) || String.Equals(x.Label, text));
			if(matched == null)
				matched = all.FirstOrDefault(x => string.Equals(x.Text, text, StringComparison.OrdinalIgnoreCase)
				                                          || string.Equals(x.Label, text, StringComparison.OrdinalIgnoreCase));
			//Returns a query even if it is not working
			if (matched == null)
				return x => x.Text(text);

			int index = Array.IndexOf(all, matched);
			return x => x.All().Index(index);
		}


		[Given(@"I have launched Phoneword app successfully")]
        public void GivenIHaveLaunchedPhonewordAppSuccessfully()
        {
            bool isVisible = screen.IsLoaded();
			Assert.IsTrue(isVisible);
        }

        [Given(@"I have entered phone word of ""(.*)""")]
        public void GivenIHaveEnteredPhoneWordOf(string word)
        {
            app.ClearText(screen.Phoneword);
            app.EnterText(screen.Phoneword, word);
            app.DismissKeyboard();
		}

        [When(@"I press Translate button")]
        public void WhenIPressTranslateButton()
        {
            app.Tap(screen.Translate);
        }

        [Then(@"I can see the phone number from the phone text")]
        public void ThenICanSeeThePhoneNumberFromThePhoneText()
        {
			string word = app.Query(screen.Phoneword)[0].Text;
			string callText = app.Query(screen.Call)[0].Text;
			Assert.IsTrue(callText.StartsWith("Call "));
			Assert.AreEqual(callText.Length, word.Length + 5);
        }

		[When(@"I tap the call button")]
		public void WhenITapTheCallButton()
		{
			app.Tap(screen.Call);
		}

		[When(@"I tap the NO button")]
		public void WhenITapTheNOButton()
		{
			app.Tap(InsensitiveText("NO"));
		}

		[Then(@"I can see the popup is dismissed")]
		public void ThenICanSeeThePopupIsDismissed()
		{
			Assert.IsFalse(InsensitiveText("NO").IsElementVisible());
		}

	}
}
