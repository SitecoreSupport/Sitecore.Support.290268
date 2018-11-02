namespace Sitecore.Support.Shell.Controls.TreeviewExFix
{
using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Globalization;
using Sitecore.IO;
using Sitecore.Links;
using Sitecore.Resources.Media;
using Sitecore.Shell.Framework;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Pages;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.WebControls;
using System;


  public class TreeviewExPage : Page
  {


    protected void Page_Load(object sender, EventArgs e)
    {
      Language language;
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(e, "e");
      Sitecore.Support.Web.UI.WebControls.TreeviewEx child = MainUtil.GetBool(WebUtil.GetQueryString("mr"), false) ? new Sitecore.Support.Web.UI.WebControls.MultiRootTreeview() : new Sitecore.Support.Web.UI.WebControls.TreeviewEx(); //Sitecore.Support.290268
      this.Controls.Add(child);
      child.ID = WebUtil.GetQueryString("treeid");
      string queryString = WebUtil.GetQueryString("db", Sitecore.Client.ContentDatabase.Name);
      Database database = Factory.GetDatabase(queryString);
      Assert.IsNotNull(database, queryString);
      ID itemId = ShortID.DecodeID(WebUtil.GetQueryString("id"));
      string str2 = WebUtil.GetQueryString("la");
      if (string.IsNullOrEmpty(str2) || !Language.TryParse(str2, out language))
      {
        language = Sitecore.Context.Language;
      }
      Item item = database.GetItem(itemId, language);
      if (item != null)
      {
        child.ParentItem = item;
      }
    }
  }
}


