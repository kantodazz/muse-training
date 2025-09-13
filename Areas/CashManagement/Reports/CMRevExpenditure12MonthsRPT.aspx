<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="CMRevExpenditure12MonthsRPT.aspx.cs" Inherits="IFMIS.Areas.CashManagement.Reports.CMRevExpenditure12MonthsRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
             <cr:crystalreportviewer id="CMRevExpenditure12Months" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
