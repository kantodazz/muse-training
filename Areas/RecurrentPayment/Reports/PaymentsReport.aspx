<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="PaymentsReport.aspx.cs" Inherits="IFMIS.Areas.RecurrentPayment.Reports.PaymentsReport" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>PaymentsReport</title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
      <CR:CrystalReportViewer ID="PaymentsReportRpt" runat="server" AutoDataBind="true" EnableDatabaseLogonPrompt="False" />
    </div>
    </form>
</body>
</html>
