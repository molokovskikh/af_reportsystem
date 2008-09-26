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

public partial class Reports_ReportMasterPage : System.Web.UI.MasterPage
{
    protected void Page_Load(object sender, EventArgs e)
    {
        SiteMap.Providers[ReportSiteMapPath.SiteMapProvider].SiteMapResolve += new SiteMapResolveEventHandler(this.ExpandForumPath);
    }

    protected SiteMapNode ExpandForumPath(Object sender, SiteMapResolveEventArgs e)
    {
        SiteMapNode currentNode = e.Provider.CurrentNode.Clone(true);

        if (currentNode.Key.Equals(e.Context.Request.ApplicationPath + "/reports/reportproperties.aspx", StringComparison.OrdinalIgnoreCase))
        {
            currentNode.ParentNode.Url += "?r=" + e.Context.Request["r"];
        }
        if (currentNode.Key.Equals(e.Context.Request.ApplicationPath + "/reports/reportpropertyvalues.aspx", StringComparison.OrdinalIgnoreCase))
        {
            currentNode.ParentNode.Url += "?r=" + e.Context.Request["r"] + "&rp=" + e.Context.Request["rp"];
        }

        return currentNode;
    }
}
