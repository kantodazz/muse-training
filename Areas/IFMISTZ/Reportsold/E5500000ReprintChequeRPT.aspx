<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="E5500000ReprintChequeRPT.aspx.cs" Inherits="IFMIS.Areas.IFMISTZ.Reports.E5500000ReprintChequeRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>

<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Reprint StandardChequeR<title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="E5500000ReprintCheque" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
