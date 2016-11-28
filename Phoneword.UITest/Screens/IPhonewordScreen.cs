using System;
using Xamarin.UITest.Queries;

namespace Phoneword.UITest
{
	public interface IPhonewordScreen : IBaseScreen
	{
		Func<AppQuery, AppQuery> Phoneword { get; }
		Func<AppQuery, AppQuery> Translate { get; }
		Func<AppQuery, AppQuery> Call { get; }
	}

	public class PhonewordScreen : BaseScreen, IPhonewordScreen
	{
		public virtual Func<AppQuery, AppQuery> Phoneword
		{
			get { return x => x.TextField().Index(0); }
		}

		public virtual Func<AppQuery, AppQuery> Translate
		{
			get { return x => x.Button().Index(0); }
		}

		public virtual Func<AppQuery, AppQuery> Call
		{
			get { return x => x.Class("UIButtonLabel").Index(1);}
		}

		public override bool IsLoaded(Func<AppQuery, AppQuery> query = null)
		{
			return base.IsLoaded(query ?? Translate);
		}
	}

	/// <summary>
	/// A class to demonstrate how to locate elements with different means on different platforms
	/// </summary>
	public class AndroidPhonewordScreen : PhonewordScreen
	{
		public override Func<AppQuery, AppQuery> Call
		{
			get { return x => x.Button().Index(1); }
		}

		public override Func<AppQuery, AppQuery> Translate
		{
			get { return x => x.Marked("Translate"); }
		}

	}
}
