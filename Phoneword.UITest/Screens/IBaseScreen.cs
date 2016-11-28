using System;
using TechTalk.SpecFlow;
using Xamarin.UITest;
using Xamarin.UITest.Queries;

namespace Phoneword.UITest
{
	public interface IBaseScreen
	{
		bool IsLoaded(Func<AppQuery, AppQuery> query = null);
	}

	public class BaseScreen
	{
		public static IApp App
		{
			get { return FeatureContext.Current.Get<IApp>(); }
		}

		public virtual bool IsLoaded(Func<AppQuery, AppQuery> query = null)
		{
			if (App == null) return false;
			if (query == null) return true;
			return query.IsElementVisible();
		}
	}
}
