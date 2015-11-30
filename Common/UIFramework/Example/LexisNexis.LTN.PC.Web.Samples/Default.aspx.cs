using System;
using System.Globalization;
using System.Web.UI;

namespace LexisNexis.LTN.PC.Web.Samples
{
    public partial class Default : Page
    {
        /// <summary>
        /// Handles the Load event of the Page control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        protected void Page_Load(object sender, EventArgs e)
        {
            var module = Request.QueryString["mod"];
            module = !string.IsNullOrWhiteSpace(module) ? module : "analytics"; //if module is empty load analytics by default
            iframe.Src = string.Format(CultureInfo.CurrentUICulture, "/app/{0}/app.html", module);
        }
    }
}