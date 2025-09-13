<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ALS_IndividualLoanhcmisRPT.aspx.cs" Inherits="IFMIS.Areas.ALS.Reports.ALS_IndividualLoanhcmisRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ALS_IndividualLoanhcmisRPT</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="ALS_IndividualLoanhcmis" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
