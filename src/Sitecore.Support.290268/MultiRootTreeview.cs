using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Text;
using Sitecore.Web;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Web.UI.Sheer;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Web.UI;

namespace Sitecore.Support.Web.UI.WebControls
{
  class MultiRootTreeview : Sitecore.Support.Web.UI.WebControls.TreeviewEx
  {



    private string currentDataContext;
    private DataContext initialDataContext;

    /// <summary>
    /// Gets or sets the current data context.
    /// </summary>
    /// <value>The current data context.</value>
    [NotNull]
    public string CurrentDataContext
    {
      get
      {
        return !string.IsNullOrEmpty(this.currentDataContext) ? this.currentDataContext : WebUtil.GetFormValue(this.ID + "_cur_datacontext");
      }

      set
      {
        this.currentDataContext = value;
      }
    }


    /// <summary>
    /// Gets or sets a value indicating whether the treeview allows dragging.
    /// </summary>
    /// <value>
    /// 	<c>true</c> if the treeview allows dragging; otherwise, <c>false</c>.
    /// </value>
    public override bool AllowDragging
    {
      get
      {
        return false;
      }

      set
      {
        throw new NotImplementedException();
      }
    }

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Init"/> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
    protected override void OnInit(EventArgs e)
    {
      base.OnInit(e);
      if (!this.Page.ClientScript.IsClientScriptIncludeRegistered("MultiRootTreeview"))
      {
        this.Page.ClientScript.RegisterClientScriptInclude(
          "MultiRootTreeview", "/sitecore/shell/Controls/MultiRootTreeview/MultiRootTreeview.js");
      }
    }

    /// <summary>Gets the data context.</summary>
    /// <returns>The data context.</returns>
    [CanBeNull]
    protected override DataContext GetDataContext()
    {
      var contexts = this.GetDataContexts();
      var currentContext = this.CurrentDataContext;
      var dataContext = contexts.FirstOrDefault(c => currentContext.Equals(c.ID, StringComparison.InvariantCultureIgnoreCase));
      if (dataContext != null)
      {
        return dataContext;
      }

      return contexts.Count > 0 ? contexts[0] : null;
    }

    /// <summary>
    /// The get data contexts.
    /// </summary>
    /// <returns>
    /// </returns>
    [NotNull]
    protected virtual ReadOnlyCollection<DataContext> GetDataContexts()
    {
      var contexts = new ListString(this.DataContext);
      var result = new List<DataContext>(contexts.Count);
      result.AddRange(contexts.Select(context => Sitecore.Context.ClientPage.FindSubControl(context)).OfType<DataContext>());
      return result.AsReadOnly();
    }

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.Load"/> event.
    /// </summary>
    /// <param name="e">
    /// The <see cref="T:System.EventArgs"/> object that contains the event data.
    /// </param>
    protected override void OnLoad([NotNull] EventArgs e)
    {
      // Do nothing. Do not call base method.
      // If datacontexts are added dynamically during page's load event, 
      // they won't be accessible during control's load event yet
      // Performing needed actions (adding handlers to datacontext event) in OnPreRender instead.
    }

    /// <summary>
    /// Raises the <see cref="E:System.Web.UI.Control.PreRender"/> event.
    /// </summary>
    /// <param name="e">An <see cref="T:System.EventArgs"/> object that contains the event data.</param>
    protected override void OnPreRender(EventArgs e)
    {
      Assert.ArgumentNotNull(e, "e");
      var dataContexts = this.GetDataContexts();
      if (dataContexts.Count == 0)
      {
        return;
      }

      foreach (var dataContext in dataContexts)
      {
        dataContext.Changed += this.DataContext_Changed;
      }

      base.OnPreRender(e);
    }

    /// <summary>
    /// Datas the context_ changed.
    /// </summary>
    /// <param name="sender">The sender.</param>
    protected void DataContext_Changed(object sender)
    {
      var context = sender as DataContext;
      if (context == null)
      {
        return;
      }

      if (!string.IsNullOrEmpty(context.ID))
      {
        var parameters = this.GetParameters(context);
        SheerResponse.SetAttribute(this.ID + "_" + context.ID, "value", parameters);
      }

      var current = this.GetDataContext();
      if (current != null && current.ID == context.ID)
      {
        this.UpdateFromDataContext(current);
      }
    }

    /// <summary>
    /// Renders the control to the specified HTML writer.
    /// </summary>
    /// <param name="output">
    /// The <see cref="T:System.Web.UI.HtmlTextWriter"/> object that receives the control content.
    /// </param>
    protected override void Render([NotNull] HtmlTextWriter output)
    {
      Assert.ArgumentNotNull(output, "output");

      Item parent = this.ParentItem;

      if (parent != null)
      {
        this.RenderChildren(output, parent);
        return;
      }

      var dataContexts = this.GetDataContexts();
      if (dataContexts.Count == 0)
      {
        return;
      }

      var dataContext = this.GetDataContext();
      if (dataContext == null)
      {
        return;
      }

      // Persisting initial data context in order to we could refer it from Render methods.
      // CurrentDataContext is changed when rendering different roots.
      this.initialDataContext = dataContext;
      this.RenderTreeBegin(output);
      var innerOutput = new HtmlTextWriter(new StringWriter(new StringBuilder(1024)));
      foreach (var dContext in this.GetDataContexts())
      {
        Item root, folder;
        dContext.GetState(out root, out folder);
        this.UpdateFromDataContext(dContext);
        if (dContext == this.initialDataContext)
        {
          this.RenderTreeState(output, folder);
          this.RenderDataContextsState(output);
        }

        var filter = this.GetFilter();
        this.Render(innerOutput, dContext.DataView, filter, root, folder);
      }

      output.Write(innerOutput.InnerWriter.ToString());

      // Restoring initial data context
      this.UpdateFromDataContext(this.initialDataContext);
      this.RenderTreeEnd(output);
    }

    /// <summary>
    /// The render data contexts state.
    /// </summary>
    /// <param name="output">
    /// The output.
    /// </param>
    protected virtual void RenderDataContextsState([NotNull] HtmlTextWriter output)
    {
      output.Write("<div class='scDataContexts' style='display:none'>");
      foreach (var dataContext in this.GetDataContexts())
      {
        output.Write("<input type='hidden' id=\"{0}\" data-context-id=\"{1}\" value=\"{2}\" />",
          this.ID + "_" + dataContext.ID,
          dataContext.ID,
          this.GetParameters(dataContext)
          );
      }

      output.Write("</div>");
      var currentDataContext = this.GetDataContext();
      if (currentDataContext != null)
      {
        output.Write("<input type='hidden' id=\"{0}_cur_datacontext\" value=\"{1}\" />", this.ID, currentDataContext.ID);
      }
    }

    /// <summary>
    /// Renders the node begin.
    /// </summary>
    /// <param name="output">The output.</param>
    /// <param name="dataView">The data view.</param>
    /// <param name="filter">The filter.</param>
    /// <param name="item">The item.</param>
    /// <param name="active">if set to <c>true</c> [active].</param>
    /// <param name="isExpanded">if set to <c>true</c> [is expanded].</param>
    protected override void RenderNodeBegin(
      [NotNull] HtmlTextWriter output,
      [NotNull] IDataView dataView,
      [NotNull] string filter,
      [NotNull] Item item,
      bool active,
      bool isExpanded)
    {
      var curDataContext = this.GetDataContext();
      active = active && curDataContext == this.initialDataContext;
      base.RenderNodeBegin(output, dataView, filter, item, active, isExpanded);
    }

    /// <summary>Gets the node ID.</summary>
    /// <param name="shortID">The short ID.</param>
    /// <returns>The node ID.</returns>
    [CanBeNull]
    protected override string GetNodeID([NotNull] string shortID)
    {
      Assert.ArgumentNotNullOrEmpty(shortID, "shortID");
      var curContext = this.CurrentDataContext;
      return this.ID + "_" + (curContext != null ? (curContext + "_") : string.Empty) + shortID;
    }

    /// <summary>
    /// The get parameters.
    /// </summary>
    /// <param name="dataContext">
    /// The data context.
    /// </param>
    /// <param name="parameters">
    /// The parameters.
    /// </param>
    /// <returns>
    /// The get parameters.
    /// </returns>
    [NotNull]
    protected virtual string GetParameters([NotNull] DataContext dataContext, [NotNull] string parameters)
    {
      var @params = new UrlString(parameters);
      @params["dv"] = dataContext.DataViewName;
      @params["fi"] = dataContext.Filter;
      return @params.ToString();
    }

    /// <summary>
    /// The get parameters.
    /// </summary>
    /// <param name="dataContext">
    /// The data context.
    /// </param>
    /// <returns>
    /// The get parameters.
    /// </returns>
    [NotNull]
    protected virtual string GetParameters([NotNull] DataContext dataContext)
    {
      return this.GetParameters(dataContext, dataContext.Parameters);
    }

    /// <summary>
    /// Updates from data context.
    /// </summary>
    /// <param name="dataContext">The data context.</param>
    protected override void UpdateFromDataContext([NotNull] DataContext dataContext)
    {
      base.UpdateFromDataContext(dataContext);
      var currentContext = this.CurrentDataContext;
      if (!currentContext.Equals(dataContext.ID, StringComparison.InvariantCultureIgnoreCase))
      {
        this.CurrentDataContext = dataContext.ID;
        SheerResponse.SetAttribute(this.ID + "_cur_datacontext", "value", dataContext.ID);
      }
    }
  }
}

