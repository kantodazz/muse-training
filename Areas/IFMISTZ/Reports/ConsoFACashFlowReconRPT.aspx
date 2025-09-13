<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ConsoFACashFlowReconRPT.aspx.cs" Inherits="IFMIS.Areas.IFMISTZ.Reports.ConsoFACashFlowReconRPT" %>
<%@ Register assembly="CrystalDecisions.Web, Version=13.0.3500.0, Culture=neutral, PublicKeyToken=692fbea5521e1304" namespace="CrystalDecisions.Web" tagprefix="CR" %>
<!DOCTYPE html>

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Consolidated CashFlow Reconciliation</title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <cr:crystalreportviewer id="ConsoFACashFlowRecon" runat="server" autodatabind="true" enabledatabaselogonprompt="False" />
        </div>
    </form>
</body>
</html>
