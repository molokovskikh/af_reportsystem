<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_base" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="base.aspx.cs" %>

<asp:Content runat="server" ID="ReportBaseContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center" style="font-size: 10pt">
        <asp:HyperLink ID="hlReportTypes" runat="server" NavigateUrl="ReportTypes.aspx">�������������� ����� �������</asp:HyperLink><br />
        <asp:HyperLink ID="hlEnums" runat="server" NavigateUrl="PropertyEnums.aspx">�������������� ������������ �����</asp:HyperLink><br />
        <asp:HyperLink ID="hlReports" runat="server" NavigateUrl="GeneralReports.aspx">�������������� �������</asp:HyperLink><br />
        <asp:HyperLink ID="hlTemplateReports" runat="server" NavigateUrl="TemplateReports.aspx">�������������� ��������</asp:HyperLink><br />
        <asp:HyperLink ID="hlTemporaryReport" runat="server" NavigateUrl="TemporaryReport.aspx">������ �������� ������</asp:HyperLink>
    </div>
</asp:Content>