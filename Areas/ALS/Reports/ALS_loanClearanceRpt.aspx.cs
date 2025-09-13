//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Web;
//using System.Web.UI;
//using System.Web.UI.WebControls;
using CrystalDecisions.CrystalReports.Engine;
using System.Linq;
using System;
using System.Web.UI;

namespace IFMIS.Areas.IFMISTZ.Reports
{
    public partial class ALS_loanClearanceRpt : Page
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

            report.Load(Server.MapPath("AlsStopDedRPT1.rpt"));
            report.SetParameterValue("@Check_no", "110750203");// Request.QueryString["check -no"]);
            report.SetParameterValue("@Loan_code", "863");// Request.QueryString["loan -code"]);
            loanClearanceByEmployeeRPT.ReportSource = report;
        }
        protected void Page_Unload(object sender, EventArgs e)
        {
            report.Close();
            report.Dispose();
        }
    }
}