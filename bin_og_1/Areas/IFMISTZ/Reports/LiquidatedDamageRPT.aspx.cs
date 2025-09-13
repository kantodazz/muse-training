using CrystalDecisions.CrystalReports.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace IFMIS.Areas.IFMISTZ.Reports
{
    public partial class LiquidatedDamageRPT : System.Web.UI.Page
    {
        ReportDocument report = new ReportDocument();
        protected void Page_Load(object sender, EventArgs e)
        {
            var encryptedQueryString = Request.QueryString["rpt"].Replace(" ", "+");
            var decryptedQueryString = QueryStringModule.Decrypt(encryptedQueryString);
            var queryStringParameters = decryptedQueryString.Split('&');
            var queryString = queryStringParameters
                .Select(queryParam => queryParam.Split('='))
                .ToDictionary(query => query[0], query => query[1]);
            {
                report.Load(Server.MapPath("LIquidatedMovementP.rpt"));
                report.SetParameterValue("@InstitutionCode", queryString["inst-code"]);
                report.SetParameterValue("@RequiredDate", queryString["required-date"]);
                LiquidatedDamage.ReportSource = report;
            }
        }
        protected void Page_Unload(object sender, EventArgs e)
        {
            report.Close();
            report.Dispose();
            LiquidatedDamage.Dispose();
        }
    }
}