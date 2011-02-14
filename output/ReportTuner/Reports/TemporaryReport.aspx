<%@ Page Title="" Language="C#" MasterPageFile="~/Reports/ReportMasterPage.master" AutoEventWireup="true" CodeBehind="TemporaryReport.aspx.cs" Inherits="ReportTuner.Reports.TemporaryReport" Theme="Main"%>
<asp:Content ID="TemporaryReportContent" ContentPlaceHolderID="ReportContentPlaceHolder" runat="server">
    <div align="center">
    <table >
        <tr align="left">
            <td width="20%">Тип отчета:</td>            
            <td >
                <asp:DropDownList ID="ddlReportTypes" runat="server" AutoPostBack="True" 
                    onselectedindexchanged="ddlReportTypes_SelectedIndexChanged"></asp:DropDownList>
            </td>
        </tr>
        <tr align="left">
            <td width="20%">Шаблон:</td>            
            <td >
                <asp:DropDownList ID="ddlTemplates" runat="server"></asp:DropDownList>
            </td>
        </tr>
        <tr align="left">
            <td width="20%">Наименование:</td>            
            <td >
                <asp:TextBox ID="tbReportName" runat="server"></asp:TextBox>
                <asp:RequiredFieldValidator ID="rfvReportName" runat="server" 
                    ControlToValidate="tbReportName" Display="Dynamic" 
                    ErrorMessage="Не установлено наименование отчета" ToolTip="Не установлено наименование отчета">*</asp:RequiredFieldValidator>
            </td>
        </tr>
    </table>    
    </div>
    <asp:Button ID="btnNext" runat="server" Text="Далее" style="float:right" 
        onclick="btnNext_Click"/>
    <div align="center">
        <asp:ValidationSummary ID="ValidationSummary" runat="server"/>
    </div>
</asp:Content>
