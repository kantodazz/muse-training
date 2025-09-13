<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ConsoIstitutionTrackerStatusRPT.aspx.cs" Inherits="IFMIS.Areas.IFMISTZ.Reports.ConsoIstitutionTrackerStatusRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Istitution Tracker RPT</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="ConsoIstitutionTrackerStatus" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
