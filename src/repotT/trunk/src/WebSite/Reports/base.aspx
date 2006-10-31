<%@ Page Language="C#" AutoEventWireup="true" CodeFile="base.aspx.cs" Inherits="Reports_base" Theme="Main" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml" >
<head runat="server">
    <title>Настройка отчетов</title>
</head>
<body>
    <form id="form1" runat="server">
    <div align="center" style="font-size: 10pt">
        <asp:HyperLink ID="hlReports" runat="server" NavigateUrl="ReportTypes.aspx">Редактирование типов отчетов</asp:HyperLink><br />
        <asp:HyperLink ID="hlEnums" runat="server" NavigateUrl="PropertyEnums.aspx">Редактирование перечислимых типов</asp:HyperLink></div>
    </form>
</body>
</html>
