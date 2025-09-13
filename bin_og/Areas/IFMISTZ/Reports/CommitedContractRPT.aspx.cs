using CrystalDecisions.CrystalReports.Engine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace IFMIS.Areas.IFMISTZ.Reports
{
    public partial class CommitedContractRPT : System.Web.UI.Page
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
                report.Load(Server.MapPath("CommitedContractRPT.rpt"));
                report.SetParameterValue("@MainInstitutionCode", queryString["inst-code"]);
                report.SetParameterValue("@FundCategoryId", queryString["fundCategoryId"]);
                report.SetParameterValue("@CostCenter", queryString["cost-center"]);
                report.SetParameterValue("@Currency", queryString["currency"]);
                report.SetParameterValue("@RequiredDate", queryString["req-date"]);
                //report.SetParameterValue("@EndDate", queryString["end-date"]);
                CommitedContract.ReportSource = report;
            }
            
        }
        protected void Page_Unload(object sender, EventArgs e)
        {
            report.Close();
            report.Dispose();
            CommitedContract.Dispose();
        }
    }
}