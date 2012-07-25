using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using Castle.ActiveRecord;
using NHibernate;

namespace ReportTuner.Helpers
{
	public class BasePage : Page
	{
		protected ISession DbSession;

		public BasePage()
		{
			Load += (sender, args) => {
				var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
				DbSession = sessionHolder.CreateSession(typeof(ActiveRecordBase));
			};

			Unload += (sender, args) => {
				if (DbSession != null)
				{
					var sessionHolder = ActiveRecordMediator.GetSessionFactoryHolder();
					sessionHolder.ReleaseSession(DbSession);
				}
			};
		}
	}
}