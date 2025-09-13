<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="E2600000ReprintChequeRPT.aspx.cs" Inherits="IFMIS.Areas.IFMISTZ.Reports.E2600000ReprintChequeRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>ReprintStandardChequeRPT</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="E2600000ReprintCheque" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
