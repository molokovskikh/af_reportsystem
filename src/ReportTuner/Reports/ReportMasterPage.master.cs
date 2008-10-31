using System;
using System.Data;
using System.Configuration;
using System.Collections;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Web.UI.WebControls.WebParts;
using System.Web.UI.HtmlControls;
using ReportTuner.Models;
using NHibernate.Criterion;

public partial class Reports_ReportMasterPage : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        SiteMap.Providers[ReportSiteMapPath.SiteMapProvider].SiteMapResolve += new SiteMapResolveEventHandler(this.ExpandForumPath);
    }

    protected SiteMapNode ExpandForumPath(Object sender, SiteMapResolveEventArgs e)
    {
        SiteMapNode currentNode = e.Provider.CurrentNode.Clone(true);

        if (currentNode.Key.EndsWith("/reports/reportproperties.aspx", StringComparison.OrdinalIgnoreCase))
        {
			if (!String.IsNullOrEmpty(e.Context.Request["TemporaryId"]))
			{
				
				SiteMapNode _temporaryNode = e.Provider.FindSiteMapNode("~/Reports/TemporaryReport.aspx");
				currentNode = _temporaryNode.ChildNodes[0].Clone(true);
				currentNode.ParentNode.Url += "?TemporaryId=" + e.Context.Request["TemporaryId"];
			}
			else
				currentNode.ParentNode.Url += "?r=" + e.Context.Request["r"];

        }
        if (currentNode.Key.EndsWith("/reports/reportpropertyvalues.aspx", StringComparison.OrdinalIgnoreCase))
        {
			if (!String.IsNullOrEmpty(e.Context.Request["TemporaryId"]))
			{

				SiteMapNode _temporaryNode = e.Provider.FindSiteMapNode("~/Reports/TemporaryReport.aspx");
				//Здесь это делается не совсем корректно.
				currentNode = _temporaryNode.ChildNodes[0].ChildNodes[0].Clone(true);
				currentNode.ParentNode.ParentNode.Url += "?TemporaryId=" + e.Context.Request["TemporaryId"];
				currentNode.ParentNode.Url += e.Context.Request["TemporaryId"] + "&rp=" + e.Context.Request["rp"];
			}
			else
				currentNode.ParentNode.Url += "?r=" + e.Context.Request["r"] + "&rp=" + e.Context.Request["rp"];
        }

		if (currentNode.Key.EndsWith("/reports/temporaryreportschedule.aspx", StringComparison.OrdinalIgnoreCase))
		{
			currentNode.ParentNode.ParentNode.Url += "?TemporaryId=" + e.Context.Request["TemporaryId"];
			Report _temporaryReport = Report.FindFirst(
				Expression.Eq("GeneralReport",
					GeneralReport.Find(Convert.ToUInt64(e.Context.Request["TemporaryId"]))
				)
			);
			currentNode.ParentNode.Url += e.Context.Request["TemporaryId"] + "&rp=" + _temporaryReport.Id; 
		}

        return currentNode;
    }
}

