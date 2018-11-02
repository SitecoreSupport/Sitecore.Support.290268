namespace Sitecore.Support.Web.UI.WebControls
{
  using Sitecore.Collections;
  using Sitecore.Data;
  using Sitecore.Data.Events;
  using Sitecore.Data.Items;
  using Sitecore.Data.Managers;
  using Sitecore.Data.Templates;
  using Sitecore.Diagnostics;
  using Sitecore.Globalization;
  using Sitecore.Resources;
  using Sitecore.Shell;
  using Sitecore.Shell.Framework;
  using Sitecore.Shell.Framework.Commands;
  using Sitecore.Sites;
  using Sitecore.Text;
  using Sitecore.Web;
  using Sitecore.Web.UI.HtmlControls;
  using Sitecore.Web.UI.HtmlControls.Data;
  using Sitecore.Web.UI.Sheer;
  using Sitecore.Web.UI.WebControls;
  using System;
  using System.Collections.Generic;
  using System.IO;
  using System.Web;
  using System.Web.UI;
  using System.Web.UI.WebControls;


  public class TreeviewEx : System.Web.UI.WebControls.WebControl
  {

    /// <summary>The updated items.</summary>
    private readonly List<Item> updatedItems = new List<Item>();

    /// <summary>The _parent item.</summary>
    private Item parentItem;

    /// <summary>The _selected.</summary>
    private List<string> selected;

    /// <summary>The enabled items template ids.</summary>
    private List<ID> enabledItemsTemplateIds;

    /// <summary>Field that will be used for display name.</summary>
    private string displayFieldName;

    /// <summary>
    /// Gets or sets a value indicating whether the treeview allows dragging.
    /// </summary>
    /// <value><c>true</c> if the treeview allows dragging; otherwise, <c>false</c>.</value>
    public virtual bool AllowDragging
    {
      get
      {
        object obj = this.ViewState["AllowDragging"];
        return obj == null || MainUtil.GetBool(obj, true);
      }
      set
      {
        this.ViewState["AllowDragging"] = value;
      }
    }

    /// <summary>
    ///  Gets or sets the click action.
    /// </summary>
    public string Click
    {
      get
      {
        return StringUtil.GetString(this.ViewState["Click"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["Click"] = value;
      }
    }

    /// <summary>
    ///  Gets or sets the context menu action.
    /// </summary>
    public string ContextMenu
    {
      get
      {
        return StringUtil.GetString(this.ViewState["ContextMenu"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["ContextMenu"] = value;
      }
    }

    /// <summary>
    /// Gets or sets the data context.
    /// </summary>
    /// <value>The data context.</value>
    public string DataContext
    {
      get
      {
        return StringUtil.GetString(this.ViewState["DataContext"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["DataContext"] = value;
      }
    }

    /// <summary>
    ///  Gets or sets the click action.
    /// </summary>
    public string DblClick
    {
      get
      {
        return StringUtil.GetString(this.ViewState["DblClick"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["DblClick"] = value;
      }
    }

    /// <summary>
    /// Gets or sets field that will be used as source for ListItem header. If empty- DisplayName will be used.
    /// </summary>
    public string DisplayFieldName
    {
      get
      {
        if (string.IsNullOrEmpty(this.displayFieldName))
        {
          this.displayFieldName = WebUtil.GetFormValue(this.ID + "_displayFieldName");
        }
        return this.displayFieldName ?? string.Empty;
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.displayFieldName = value;
      }
    }

    /// <summary>
    ///  Sets the the template id for the items that are displayed as enabled. If not set all items are considered to be enabled 
    /// </summary>
    [Obsolete("Use EnabledItemsTemplateIds instead")]
    public ID EnabledItemsTemplateId
    {
      protected get
      {
        List<ID> list = this.EnabledItemsTemplateIds;
        if (list.Count <= 0)
        {
          return Sitecore.Data.ID.Null;
        }
        return list[0];
      }
      set
      {
        Assert.IsNotNull(value, "value");
        this.EnabledItemsTemplateIds = new List<ID>
                {
                    value
                };
      }
    }

    /// <summary>
    /// Gets or sets the enabled items template ids.
    /// </summary>
    /// <value>The enabled items template ids.</value>
    public List<ID> EnabledItemsTemplateIds
    {
      protected get
      {
        if (this.enabledItemsTemplateIds != null)
        {
          return this.enabledItemsTemplateIds;
        }
        List<ID> list = new List<ID>();
        string formValue = WebUtil.GetFormValue(this.ID + "_templateIDs");
        if (string.IsNullOrEmpty(formValue))
        {
          return list;
        }
        ListString listString = new ListString(formValue);
        foreach (string current in listString)
        {
          ID item;
          if (Sitecore.Data.ID.TryParse(current, out item))
          {
            list.Add(item);
          }
        }
        this.enabledItemsTemplateIds = list;
        return list;
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.enabledItemsTemplateIds = value;
      }
    }

    /// <summary>
    /// Gets or sets the parent item.
    /// </summary>
    /// <value>The parent item.</value>
    public Item ParentItem
    {
      get
      {
        return this.parentItem;
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.parentItem = value;
      }
    }

    /// <summary>
    /// Gets the selected Ids.
    /// </summary>
    /// <value>The selected Ids.</value>
    public List<string> SelectedIDs
    {
      get
      {
        if (this.selected == null)
        {
          this.selected = this.GetSelectedIDs();
        }
        return this.selected;
      }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the treeview shows the root item.
    /// </summary>
    /// <value><c>true</c> if the treeview shows the root item; otherwise, <c>false</c>.</value>
    public bool ShowRoot
    {
      get
      {
        object obj = this.ViewState["ShowRoot"];
        return obj == null || MainUtil.GetBool(obj, true);
      }
      set
      {
        this.ViewState["ShowRoot"] = value;
      }
    }

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    /// <value>The parameters.</value>
    private string DataViewName
    {
      get
      {
        return StringUtil.GetString(this.ViewState["DataViewName"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["DataViewName"] = value;
      }
    }

    /// <summary>
    /// Gets or sets the filter.
    /// </summary>
    /// <value>The filter.</value>
    private string Filter
    {
      get
      {
        return StringUtil.GetString(this.ViewState["Filter"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["Filter"] = value;
      }
    }

    /// <summary>
    /// Gets or sets the parameters.
    /// </summary>
    /// <value>The parameters.</value>
    private string Parameters
    {
      get
      {
        return StringUtil.GetString(this.ViewState["Parameters"]);
      }
      set
      {
        Assert.ArgumentNotNull(value, "value");
        this.ViewState["Parameters"] = value;
      }
    }

    /// <summary>Gets the selected items.</summary>
    /// <param name="language">The language.</param>
    /// <param name="version">The version.</param>
    /// <returns>The selected items</returns>
    public Item[] GetSelectedItems(Language language, Sitecore.Data.Version version)
    {
      List<Item> list = new List<Item>();
      IDataView dataView = this.GetDataView();
      if (dataView == null)
      {
        return list.ToArray();
      }
      foreach (string current in this.GetSelectedIDs())
      {
        if (!string.IsNullOrEmpty(current) && !(current == this.ID))
        {
          string id = ShortID.Decode(current);
          Item item = dataView.GetItem(id, language, version);
          if (item != null)
          {
            list.Add(item);
          }
        }
      }
      return list.ToArray();
    }

    /// <summary>Gets the selected items.</summary>
    /// <returns>The selected items.</returns>
    public Item[] GetSelectedItems()
    {
      return this.GetSelectedItems(Language.Current, Sitecore.Data.Version.Latest);
    }

    /// <summary>Gets the selection item.</summary>
    /// <returns>The selection item.</returns>
    public virtual Item GetSelectionItem()
    {
      return this.GetSelectionItem(Language.Current, Sitecore.Data.Version.Latest);
    }

    /// <summary>Gets the selection item.</summary>
    /// <param name="language">The language.</param>
    /// <param name="version">The version.</param>
    /// <returns>The selection item.</returns>
    public Item GetSelectionItem(Language language, Sitecore.Data.Version version)
    {
      Item[] selectedItems = this.GetSelectedItems(language, version);
      if (selectedItems.Length != 0)
      {
        return selectedItems[0];
      }
      return null;
    }

    /// <summary>Refreshes the specified item.</summary>
    /// <param name="item">The item.</param>
    public void Refresh(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      string shortID = item.ID.ToShortID().ToString();
      string nodeID = this.GetNodeID(shortID);
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      this.RenderParent(htmlTextWriter, item);
      SheerResponse.SetOuterHtml(nodeID, htmlTextWriter.InnerWriter.ToString());
    }

    /// <summary>Refreshes the root.</summary>
    public void RefreshRoot()
    {
      HtmlTextWriter htmlTextWriter = new HtmlTextWriter(new StringWriter());
      this.RenderControl(htmlTextWriter);
      SheerResponse.SetOuterHtml(this.ID, htmlTextWriter.InnerWriter.ToString());
    }

    /// <summary>Refreshes the selected.</summary>
    public void RefreshSelected()
    {
      Item[] selectedItems = this.GetSelectedItems();
      for (int i = 0; i < selectedItems.Length; i++)
      {
        Item item = selectedItems[i];
        this.Refresh(item);
      }
    }

    /// <summary>Sets the selected item.</summary>
    /// <param name="item">The item.</param>
    public void SetSelectedItem(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      Item selectionItem = this.GetSelectionItem();
      this.SelectedIDs.Clear();
      this.SelectedIDs.Add(item.ID.ToShortID().ToString());
      this.SetSelectedIDs(this.SelectedIDs);
      if (selectionItem != null)
      {
        this.Refresh(selectionItem);
      }
      this.Refresh(item);
    }

    /// <summary>Drops the specified data.</summary>
    /// <param name="data">The data.</param>
    protected void Drop(string data)
    {
      if (data == null)
      {
        return;
      }
      if (!data.StartsWith("sitecore:", StringComparison.InvariantCulture))
      {
        return;
      }
      data = data.Substring(9);
      if (!data.StartsWith("item:", StringComparison.InvariantCulture))
      {
        return;
      }
      data = data.Substring(5);
      int num = data.LastIndexOf(",", StringComparison.InvariantCulture);
      if (num <= 0)
      {
        return;
      }
      string dragID = TreeviewEx.GetDragID(StringUtil.Left(data, num));
      string dragID2 = TreeviewEx.GetDragID(StringUtil.Mid(data, num + 1));
      Item item = Client.ContentDatabase.Items[dragID2];
      Item item2 = Client.ContentDatabase.Items[dragID];
      if (item != null && item2 != null)
      {
        Items.DragTo(item2, item, Sitecore.Context.ClientPage.ClientRequest.CtrlKey, !Sitecore.Context.ClientPage.ClientRequest.ShiftKey, !Sitecore.Context.ClientPage.ClientRequest.AltKey);
        return;
      }
      if (item2 == null)
      {
        SheerResponse.Alert("The source item could not be found.\n\nIt may have been deleted by another user.", new string[0]);
        return;
      }
      SheerResponse.Alert("The target item could not be found.\n\nIt may have been deleted by another user.", new string[0]);
    }

    /// <summary>Gets the context menu.</summary>
    protected virtual void GetContextMenu()
    {
      this.GetContextMenu("below-right");
    }

    /// <summary>Gets the context menu.</summary>
    /// <param name="where">The where.</param>
    protected virtual void GetContextMenu(string where)
    {
      Assert.ArgumentNotNullOrEmpty(where, "where");
      IDataView dataView = this.GetDataView();
      if (dataView == null)
      {
        return;
      }
      string source = Sitecore.Context.ClientPage.ClientRequest.Source;
      string control = Sitecore.Context.ClientPage.ClientRequest.Control;
      int num = source.LastIndexOf("_", StringComparison.InvariantCulture);
      Assert.IsTrue(num >= 0, "Invalid source ID");
      string id = ShortID.Decode(StringUtil.Mid(source, num + 1));
      Item item = dataView.GetItem(id);
      if (item == null)
      {
        return;
      }
      SheerResponse.DisableOutput();
      Sitecore.Shell.Framework.ContextMenu contextMenu = new Sitecore.Shell.Framework.ContextMenu();
      CommandContext context = new CommandContext(item);
      Sitecore.Web.UI.HtmlControls.Menu menu = contextMenu.Build(context);
      menu.AddDivider();
      menu.Add("__Refresh", "Refresh", "Office/16x16/refresh.png", string.Empty, string.Concat(new object[]
      {
                "javascript:Sitecore.Treeview.refresh(\"",
                source,
                "\",\"",
                control,
                "\",\"",
                item.ID.ToShortID(),
                "\")"
      }), false, string.Empty, MenuItemType.Normal);
      SheerResponse.EnableOutput();
      SheerResponse.ShowContextMenu(control, where, menu);
    }
    #region Sitecore.Support.290268
    /// <summary>
    /// Gets the header value.
    /// </summary>
    /// <param name="item">The item.</param>
    /// <returns>Header text for list item.</returns>
    protected virtual string GetHeaderValue(Item item)
    {
      if (UserOptions.View.UseDisplayName) //Added checking of user options. Sitecore.Support.290268
      {
        Assert.ArgumentNotNull(item, "item");
        string text = string.IsNullOrEmpty(this.DisplayFieldName) ? item.DisplayName : item[this.DisplayFieldName];
        return string.IsNullOrEmpty(text) ? item.DisplayName : text;
      }
      else
      {
        Assert.ArgumentNotNull(item, "item");
        string text = string.IsNullOrEmpty(this.DisplayFieldName) ? item.Name : item[this.DisplayFieldName];
        return string.IsNullOrEmpty(text) ? item.DisplayName : text;
      }
    }
    #endregion
    /// <summary>
    ///             Gets the name of the tree node.
    /// </summary>
    /// <param name="item">
    /// The item.
    /// </param>
    /// <returns>
    /// The name of the tree node.
    /// </returns>
    protected virtual string GetTreeNodeName(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      return Assert.ResultNotNull<string>(item.Appearance.DisplayName);
    }

    /// <summary>Raises the <see cref="E:System.Web.UI.Control.Init" /> event.</summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnInit(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnInit(e);
      this.Page.ClientScript.RegisterClientScriptInclude("TreeviewEx", "/sitecore/shell/Controls/TreeviewEx/TreeviewEx.js");
      SiteContext site = Sitecore.Context.Site;
      if (site == null)
      {
        return;
      }
      site.Notifications.ItemCopied += delegate (object sender, ItemCopiedEventArgs args)
      {
        this.AddUpdatedItem(args.Copy, true);
      };
      site.Notifications.ItemCreated += delegate (object sender, ItemCreatedEventArgs args)
      {
        this.AddUpdatedItem(args.Item, true);
      };
      site.Notifications.ItemDeleted += delegate (object sender, ItemDeletedEventArgs args)
      {
        this.AddUpdatedItem(args.Item.Database.GetItem(args.ParentID), false);
      };
      site.Notifications.ItemMoved += new ItemMovedDelegate(this.ItemMovedNotification);
      site.Notifications.ItemSaved += delegate (object sender, ItemSavedEventArgs args)
      {
        this.AddUpdatedItem(args.Item, false);
      };
      site.Notifications.ItemSortorderChanged += delegate (object sender, ItemSortorderChangedEventArgs args)
      {
        this.AddUpdatedItem(args.Item, true);
      };
      site.Notifications.ItemRenamed += delegate (object sender, ItemRenamedEventArgs args)
      {
        this.AddUpdatedItem(args.Item, false);
      };
      site.Notifications.ItemChildrenChanged += delegate (object sender, ItemChildrenChangedEventArgs args)
      {
        this.AddUpdatedItem(args.Item, false);
      };
    }

    /// <summary>Raises the <see cref="E:System.Web.UI.Control.Load" /> event.</summary>
    /// <param name="e">The <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnLoad(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnLoad(e);
      DataContext dataContext = this.GetDataContext();
      if (dataContext == null)
      {
        return;
      }
      dataContext.Changed += new DataContext.DataContextChangedDelegate(this.DataContext_OnChanged);
      this.UpdateFromDataContext(dataContext);
      Item[] selectedItems = this.GetSelectedItems(dataContext.Language, Sitecore.Data.Version.Latest);
      if (selectedItems.Length == 0)
      {
        return;
      }
      dataContext.SetFolder(selectedItems[0].Uri);
    }

    /// <summary>Raises the <see cref="E:System.Web.UI.Control.PreRender" /> event.</summary>
    /// <param name="e">An <see cref="T:System.EventArgs" /> object that contains the event data.</param>
    protected override void OnPreRender(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      base.OnPreRender(e);
      foreach (Item current in this.updatedItems)
      {
        this.Refresh(current);
      }
    }

    /// <summary>Renders the control to the specified HTML writer.</summary>
    /// <param name="output">The <see cref="T:System.Web.UI.HtmlTextWriter" /> object that receives the control content.</param>
    protected override void Render(HtmlTextWriter output)
    {
      Assert.ArgumentNotNull(output, "output");
      Item item = this.ParentItem;
      if (item != null)
      {
        this.RenderChildren(output, item);
        return;
      }
      DataContext dataContext = this.GetDataContext();
      if (dataContext == null)
      {
        return;
      }
      IDataView dataView = dataContext.DataView;
      if (dataView == null)
      {
        return;
      }
      this.RenderTreeBegin(output);
      string filter = this.GetFilter();
      Item root;
      Item folder;
      dataContext.GetState(out root, out folder);
      this.RenderTreeState(output, folder);
      this.Render(output, dataView, filter, root, folder);
      this.RenderTreeEnd(output);
    }

    /// <summary>Gets the ID.</summary>
    /// <param name="id">The id.</param>
    /// <returns>The get drag id.</returns>
    private static string GetDragID(string id)
    {
      Assert.ArgumentNotNull(id, "id");
      int num = id.LastIndexOf("_", StringComparison.InvariantCulture);
      if (num >= 0)
      {
        id = StringUtil.Mid(id, num + 1);
      }
      if (ShortID.IsShortID(id))
      {
        id = ShortID.Decode(id);
      }
      return id;
    }

    /// <summary>Renders the node end.</summary>
    /// <param name="output">The output.</param>
    private static void RenderNodeEnd(HtmlTextWriter output)
    {
      Assert.ArgumentNotNull(output, "output");
      output.Write("</div>");
    }

    /// <summary>Renders the tree node glyph.</summary>
    /// <param name="output">The output.</param>
    /// <param name="dataView">The data view.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="item">The item.</param>
    /// <param name="id">The id.</param>
    /// <param name="isExpanded">if set to <c>true</c> [is expanded].</param>
    private static void RenderTreeNodeGlyph(HtmlTextWriter output, IDataView dataView, string filter, Item item, string id, bool isExpanded)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(dataView, "dataView");
      Assert.ArgumentNotNull(filter, "filter");
      Assert.ArgumentNotNull(item, "item");
      Assert.ArgumentNotNullOrEmpty(id, "id");
      ImageBuilder imageBuilder = new ImageBuilder
      {
        Class = "scContentTreeNodeGlyph"
      };
      if (dataView.HasChildren(item, filter))
      {
        if (isExpanded)
        {
          imageBuilder.Src = "images/treemenu_expanded.png";
        }
        else
        {
          imageBuilder.Src = "images/treemenu_collapsed.png";
        }
      }
      else
      {
        imageBuilder.Src = "images/noexpand15x15.gif";
      }
      output.Write(imageBuilder.ToString());
    }

    /// <summary>Renders the tree node icon.</summary>
    /// <param name="output">The output.</param>
    /// <param name="item">The item.</param>
    private static void RenderTreeNodeIcon(HtmlTextWriter output, Item item)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(item, "item");
      UrlBuilder urlBuilder = new UrlBuilder(item.Appearance.Icon);
      if (item.Paths.IsMediaItem)
      {
        urlBuilder["rev"] = item.Statistics.Revision;
        urlBuilder["la"] = item.Language.ToString();
      }
      ImageBuilder imageBuilder = new ImageBuilder
      {
        Src = urlBuilder.ToString(),
        Width = 16,
        Height = 16,
        Class = "scContentTreeNodeIcon"
      };
      if (!string.IsNullOrEmpty(item.Help.Text))
      {
        imageBuilder.Alt = item.Help.Text;
      }
      imageBuilder.Render(output);
    }

    /// <summary>Adds the updated item.</summary>
    /// <param name="item">The item.</param>
    /// <param name="updateParent">if set to <c>true</c> [update parent].</param>
    private void AddUpdatedItem(Item item, bool updateParent)
    {
      if (item == null)
      {
        return;
      }
      if (updateParent)
      {
        item = item.Parent;
        if (item == null)
        {
          return;
        }
      }
      foreach (Item current in this.updatedItems)
      {
        if (current.Axes.IsAncestorOf(item))
        {
          return;
        }
      }
      for (int i = this.updatedItems.Count - 1; i >= 0; i--)
      {
        Item item2 = this.updatedItems[i];
        if (item.Axes.IsAncestorOf(item2))
        {
          this.updatedItems.Remove(item2);
        }
      }
      this.updatedItems.Add(item);
    }

    /// <summary>Handles the data context changed event.</summary>
    /// <param name="sender">The sender.</param>
    private void DataContext_OnChanged(object sender)
    {
      DataContext dataContext = this.GetDataContext();
      if (dataContext == null)
      {
        return;
      }
      this.UpdateFromDataContext(dataContext);
    }

    /// <summary>Gets the data context.</summary>
    /// <returns>The data context.</returns>
    protected virtual DataContext GetDataContext()
    {
      return Sitecore.Context.ClientPage.FindSubControl(this.DataContext) as DataContext;
    }

    /// <summary>Gets the data view.</summary>
    /// <returns>The data view.</returns>
    public IDataView GetDataView()
    {
      string text = this.DataViewName;
      if (string.IsNullOrEmpty(text))
      {
        DataContext dataContext = this.GetDataContext();
        if (dataContext != null)
        {
          this.UpdateFromDataContext(dataContext);
        }
        text = this.DataViewName;
      }
      string text2 = this.Parameters;
      if (string.IsNullOrEmpty(text))
      {
        text2 = WebUtil.GetFormValue(this.ID + "_Parameters");
        UrlString urlString = new UrlString(text2);
        text = urlString["dv"];
      }
      return DataViewFactory.GetDataView(text, text2);
    }

    /// <summary>Gets the filter.</summary>
    /// <returns>The filter.</returns>
    protected virtual string GetFilter()
    {
      string text = this.Filter;
      if (string.IsNullOrEmpty(text))
      {
        UrlString urlString = new UrlString(WebUtil.GetFormValue(this.ID + "_Parameters"));
        text = HttpUtility.UrlDecode(StringUtil.GetString(new string[]
        {
                    urlString["fi"]
        }));
      }
      return text;
    }

    /// <summary>Gets the node ID.</summary>
    /// <param name="shortID">The short ID.</param>
    /// <returns>The node ID.</returns>
    protected virtual string GetNodeID(string shortID)
    {
      Assert.ArgumentNotNullOrEmpty(shortID, "shortID");
      return this.ID + "_" + shortID;
    }

    /// <summary>Updates the parameters.</summary>
    /// <returns>The get parameters.</returns>
    private string GetParameters()
    {
      UrlString urlString = new UrlString(this.Parameters);
      urlString["dv"] = this.DataViewName;
      urlString["fi"] = this.Filter;
      return urlString.ToString();
    }

    /// <summary>Gets the selected Ids.</summary>
    /// <returns>The selected Ids.</returns>
    private List<string> GetSelectedIDs()
    {
      return new List<string>(WebUtil.GetFormValue(this.ID + "_Selected").Split(new char[]
      {
                ','
      }));
    }

    /// <summary>Gets the style.</summary>
    /// <param name="item">The item.</param>
    /// <returns>The style.</returns>
    private string GetStyle(Item item)
    {
      Assert.ArgumentNotNull(item, "item");
      if (item.TemplateID == TemplateIDs.TemplateField)
      {
        return string.Empty;
      }
      string text = item.Appearance.Style;
      if (string.IsNullOrEmpty(text) && (item.Appearance.Hidden || item.RuntimeSettings.IsVirtual))
      {
        text = "color:#666666";
      }
      if (string.IsNullOrEmpty(text))
      {
        List<ID> list = this.EnabledItemsTemplateIds;
        if (list.Count > 0)
        {
          Template template = TemplateManager.GetTemplate(item);
          if (template != null && list.FindIndex((ID id) => template.DescendsFromOrEquals(id)) < 0)
          {
            text = "color:#666666";
          }
        }
      }
      if (!string.IsNullOrEmpty(text))
      {
        text = " style=\"" + text + "\"";
      }
      return text;
    }

    /// <summary>Called when the item is moved.</summary>
    /// <param name="sender">The sender.</param>
    /// <param name="args">The arguments.</param>
    private void ItemMovedNotification(object sender, ItemMovedEventArgs args)
    {
      Assert.ArgumentNotNull(sender, "sender");
      Assert.ArgumentNotNull(args, "args");
      this.AddUpdatedItem(args.Item, true);
      Item item = args.Item.Database.GetItem(args.OldParentID);
      if (item != null)
      {
        this.AddUpdatedItem(item, false);
      }
    }

    /// <summary>Renders the specified output.</summary>
    /// <param name="output">The output.</param>
    /// <param name="dataView">The data view.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="root">The root.</param>
    /// <param name="folder">The folder.</param>
    protected virtual void Render(HtmlTextWriter output, IDataView dataView, string filter, Item root, Item folder)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(dataView, "dataView");
      Assert.ArgumentNotNull(filter, "filter");
      Assert.ArgumentNotNull(root, "root");
      Assert.ArgumentNotNull(folder, "folder");
      if (this.ShowRoot)
      {
        this.RenderNode(output, dataView, filter, root, root, folder);
        return;
      }
      ItemCollection children = dataView.GetChildren(root, string.Empty, true, 0, 0, this.GetFilter());
      foreach (Item parent in children)
      {
        this.RenderNode(output, dataView, filter, root, parent, folder);
      }
    }

    /// <summary>
    /// Renders the tree state.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="folder">The folder.</param>
    protected virtual void RenderTreeState(HtmlTextWriter output, Item folder)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(folder, "folder");
      output.Write("<input id=\"");
      output.Write(this.ID);
      output.Write("_Selected\" type=\"hidden\" value=\"" + folder.ID.ToShortID() + "\" />");
      output.Write("<input id=\"");
      output.Write(this.ID);
      output.Write("_Database\" type=\"hidden\" value=\"" + folder.Database.Name + "\" />");
      output.Write("<input id=\"");
      output.Write(this.ID);
      output.Write("_Parameters\" type=\"hidden\" value=\"" + this.GetParameters() + "\" />");
      if (this.EnabledItemsTemplateIds.Count > 0)
      {
        ListString listString = new ListString();
        foreach (ID current in this.EnabledItemsTemplateIds)
        {
          listString.Add(current.ToString());
        }
        output.Write("<input id=\"");
        output.Write(this.ID);
        output.Write("_templateIDs\" type=\"hidden\" value=\"" + listString + "\"/>");
      }
      DataContext dataContext = this.GetDataContext();
      if (dataContext != null)
      {
        output.Write(string.Concat(new object[]
        {
                    "<input id=\"",
                    this.ID,
                    "_Language\" type=\"hidden\" value=\"",
                    dataContext.Language,
                    "\"/>"
        }));
      }
    }

    /// <summary>
    /// Renders the tree end.
    /// </summary>
    /// <param name="output">The output.</param>
    protected virtual void RenderTreeEnd(HtmlTextWriter output)
    {
      Assert.IsNotNull(output, "output");
      if (!string.IsNullOrEmpty(this.DisplayFieldName))
      {
        output.Write("<input id=\"");
        output.Write(this.ID);
        output.Write("_displayFieldName\" type=\"hidden\" value=\"" + HttpUtility.HtmlEncode(this.DisplayFieldName) + "\"/>");
      }
      output.Write("</div>");
    }

    /// <summary>
    /// Renders the tree begin.
    /// </summary>
    /// <param name="output">The output.</param>
    protected virtual void RenderTreeBegin(HtmlTextWriter output)
    {
      Assert.ArgumentNotNull(output, "output");
      output.Write("<div id=\"");
      output.Write(this.ID);
      output.Write("\" onclick=\"javascript:return Sitecore.Treeview.onTreeClick(this,event");
      if (!string.IsNullOrEmpty(this.Click))
      {
        output.Write(",'");
        output.Write(StringUtil.EscapeQuote(this.Click));
        output.Write("'");
      }
      output.Write(")\"");
      output.Write(" onkeydown=\"javascript:return Sitecore.Treeview.onKeyDown(this,event)\"");
      if (!string.IsNullOrEmpty(this.DblClick))
      {
        output.Write(" ondblclick=\"");
        output.Write(AjaxScriptManager.GetEventReference(this.DblClick));
        output.Write("\"");
      }
      if (!string.IsNullOrEmpty(this.ContextMenu))
      {
        output.Write(" oncontextmenu=\"");
        output.Write(AjaxScriptManager.GetEventReference(this.ContextMenu));
        output.Write("\"");
      }
      if (this.AllowDragging)
      {
        output.Write(" onmousedown=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" onmousemove=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" ondragstart=\"javascript:return Sitecore.Treeview.onTreeDrag(this,event)\" ondragover=\"javascript:return Sitecore.Treeview.onTreeDrop(this,event)\" ondrop=\"javascript:return Sitecore.Treeview.onTreeDrop(this,event)\"");
      }
      if (base.Style.Count > 0)
      {
        output.Write(" style='" + base.Style.Value + "'");
      }
      output.Write(">");
    }

    /// <summary>Renders the parent.</summary>
    /// <param name="output">The output.</param>
    /// <param name="parent">The parent.</param>
    protected virtual void RenderChildren(HtmlTextWriter output, Item parent)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(parent, "parent");
      IDataView dataView = this.GetDataView();
      if (dataView == null)
      {
        return;
      }
      string filter = this.GetFilter();
      ItemCollection children = dataView.GetChildren(parent, string.Empty, true, 0, 0, filter);
      if (children == null)
      {
        return;
      }
      foreach (Item item in children)
      {
        this.RenderNodeBegin(output, dataView, filter, item, false, false);
        TreeviewEx.RenderNodeEnd(output);
      }
      if (!string.IsNullOrEmpty(this.DisplayFieldName))
      {
        output.Write("<input id=\"");
        output.Write(this.ID);
        output.Write("_displayFieldName\" type=\"hidden\" value=\"" + HttpUtility.HtmlEncode(this.DisplayFieldName) + "\"/>");
      }
    }

    /// <summary>Renders the node.</summary>
    /// <param name="output">The output.</param>
    /// <param name="dataView">The data view.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="root">The root.</param>
    /// <param name="parent">The parent.</param>
    /// <param name="folder">The folder.</param>
    private void RenderNode(HtmlTextWriter output, IDataView dataView, string filter, Item root, Item parent, Item folder)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(dataView, "dataView");
      Assert.ArgumentNotNull(filter, "filter");
      Assert.ArgumentNotNull(root, "root");
      Assert.ArgumentNotNull(parent, "parent");
      Assert.ArgumentNotNull(folder, "folder");
      bool flag = parent.ID == root.ID || (parent.Axes.IsAncestorOf(folder) && parent.ID != folder.ID);
      this.RenderNodeBegin(output, dataView, filter, parent, parent.ID == folder.ID, flag);
      if (flag)
      {
        ItemCollection children = dataView.GetChildren(parent, string.Empty, true, 0, 0, this.GetFilter());
        if (children != null)
        {
          foreach (Item parent2 in children)
          {
            this.RenderNode(output, dataView, filter, root, parent2, folder);
          }
        }
      }
      TreeviewEx.RenderNodeEnd(output);
    }

    /// <summary>Renders the node begin.</summary>
    /// <param name="output">The output.</param>
    /// <param name="dataView">The data view.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="item">The item.</param>
    /// <param name="active">if set to <c>true</c> [active].</param>
    /// <param name="isExpanded">if set to <c>true</c> [is expanded].</param>
    protected virtual void RenderNodeBegin(HtmlTextWriter output, IDataView dataView, string filter, Item item, bool active, bool isExpanded)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(dataView, "dataView");
      Assert.ArgumentNotNull(filter, "filter");
      Assert.ArgumentNotNull(item, "item");
      string text = item.ID.ToShortID().ToString();
      string nodeID = this.GetNodeID(text);
      output.Write("<div id=\"");
      output.Write(nodeID);
      output.Write("\" class=\"scContentTreeNode\">");
      TreeviewEx.RenderTreeNodeGlyph(output, dataView, filter, item, text, isExpanded);
      string str = (active || this.SelectedIDs.Contains(text)) ? "scContentTreeNodeActive" : "scContentTreeNodeNormal";
      string style = this.GetStyle(item);
      output.Write("<a href=\"#\" class=\"" + str + "\"");
      if (!string.IsNullOrEmpty(item.Help.Text))
      {
        output.Write(" title=\"");
        output.Write(StringUtil.EscapeQuote(item.Help.Text));
        output.Write("\"");
      }
      output.Write(style);
      output.Write(">");
      TreeviewEx.RenderTreeNodeIcon(output, item);
      output.Write("<span hidefocus=\"true\" class=\"scContentTreeNodeTitle\" tabindex='0'>{0}</span>", this.GetHeaderValue(item));
      output.Write("</a>");
    }

    /// <summary>Renders the parent.</summary>
    /// <param name="output">The output.</param>
    /// <param name="parent">The parent.</param>
    private void RenderParent(HtmlTextWriter output, Item parent)
    {
      Assert.ArgumentNotNull(output, "output");
      Assert.ArgumentNotNull(parent, "parent");
      IDataView dataView = this.GetDataView();
      if (dataView == null)
      {
        return;
      }
      string filter = this.GetFilter();
      this.RenderNodeBegin(output, dataView, filter, parent, false, true);
      ItemCollection children = dataView.GetChildren(parent, string.Empty, true, 0, 0, filter);
      if (children != null)
      {
        foreach (Item item in children)
        {
          this.RenderNodeBegin(output, dataView, filter, item, false, false);
          TreeviewEx.RenderNodeEnd(output);
        }
      }
      TreeviewEx.RenderNodeEnd(output);
    }

    /// <summary>Sets the selected Ids.</summary>
    /// <param name="ids">The ids.</param>
    private void SetSelectedIDs(List<string> ids)
    {
      Assert.ArgumentNotNull(ids, "ids");
      SheerResponse.SetAttribute(this.ID + "_Selected", "value", StringUtil.Join(ids, ","));
    }

    /// <summary>Updates from data context.</summary>
    /// <param name="dataContext">The data context.</param>
    protected virtual void UpdateFromDataContext(DataContext dataContext)
    {
      Assert.ArgumentNotNull(dataContext, "dataContext");
      string parameters = dataContext.Parameters;
      string filter = dataContext.Filter;
      string dataViewName = dataContext.DataViewName;
      if (parameters != this.Parameters || filter != this.Filter || dataViewName != this.DataViewName)
      {
        this.Parameters = parameters;
        this.Filter = filter;
        this.DataViewName = dataViewName;
        SheerResponse.SetAttribute(this.ID + "_Parameters", "value", this.GetParameters());
      }
    }

  }

}

