using CrystalDecisions.CrystalReports.Engine;
using CrystalDecisions.Shared;
using System.Linq;
using System;
using System.Configuration;
using System.Web.UI;

namespace IFMIS.Areas.IFMISTZ.Reports
{
    public partial class ConsoBudgetByCategoryRPT : Page
    {
        ReportDocument report = new ReportDocument();

        TableLogOnInfo _crtableLogoninfo = new TableLogOnInfo();
        ConnectionInfo _crConnectionInfo = new ConnectionInfo();
        Tables _crTables;
	
        protected void Page_Load(object sender, EventArgs e)
        {
            string[] strConnection = ConfigurationManager.ConnectionStrings["IFMISTZReportsContext"].ConnectionString.Split(new char[] { ';' });

            var encryptedQueryString = Request.QueryString["rpt"].Replace(" ", "+");
            var decryptedQueryString = QueryStringModule.Decrypt(encryptedQueryString);
            var queryStringParameters = decryptedQueryString.Split('&');
            var queryString = queryStringParameters
                .Select(queryParam => queryParam.Split('='))
                .ToDictionary(query => query[0], query => query[1]);

            //if (User.Identity.IsAuthenticated)
            {
                report.Load(Server.MapPath("ConsoBudgetByCategoryRPT.rpt"));
                report.SetParameterValue("@MainInstitutionCode", queryString["inst-code"]);
                //report.SetParameterValue("@SubInstitutionCode", queryString["sub-inst-code"]);
                //report.SetParameterValue("@CostCenter", queryString["cost-center"]);
                report.SetParameterValue("@FundCategoryId", queryString["fundCategoryId"]);
                report.SetParameterValue("@Currency", queryString["currency"]);
                //report.SetParameterValue("@Period", queryString["period"]);
                report.SetParameterValue("@RequiredDate", queryString["required-date"]);

                ConsoBudgetByCategory.ReportSource = report;
            }

            _crConnectionInfo.ServerName = strConnection[0].Split(new char[] { '=' }).GetValue(1).ToString();
            _crConnectionInfo.DatabaseName = strConnection[1].Split(new char[] { '=' }).GetValue(1).ToString();
            _crConnectionInfo.UserID = strConnection[3].Split(new char[] { '=' }).GetValue(1).ToString();
            _crConnectionInfo.Password = strConnection[4].Split(new char[] { '=' }).GetValue(1).ToString();

            _crTables = report.Database.Tables;
			
            foreach (Table crTable in _crTables)
            {
                _crtableLogoninfo = crTable.LogOnInfo;
                _crtableLogoninfo.ConnectionInfo = _crConnectionInfo;
                crTable.ApplyLogOnInfo(_crtableLogoninfo);
            }
        }
        protected void Page_Unload(object sender, EventArgs e)
        {
            report.Close();
            report.Dispose();
            ConsoBudgetByCategory.Dispose();
        }
    }
}