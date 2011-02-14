<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_ReportPropertyValues" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="ReportPropertyValues.aspx.cs" %>

<asp:Content runat="server" ID="ReportPropertyValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
            <strong style="font-size:small;">Редактирование значений списка "<asp:Label ID="lblListName" runat="server" Text="Label"/>" отчета "<asp:Label ID="lblReportCaption" runat="server" Text="Label"/>" типа отчета "<asp:Label ID="lblReportType" runat="server" Text="Label"/>"</strong><br/>
        <asp:TextBox ID="tbSearch" runat="server" OnTextChanged="tbSearch_TextChanged"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" OnClick="btnSearch_Click" Text="Найти" /><br />
        &nbsp; &nbsp;&nbsp;
        <asp:Label ID="Label1" runat="server" Text="На странице:"></asp:Label><asp:DropDownList
            ID="ddlPages" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPages_SelectedIndexChanged">
            <asp:ListItem>10</asp:ListItem>
            <asp:ListItem>20</asp:ListItem>
            <asp:ListItem>50</asp:ListItem>
            <asp:ListItem>100</asp:ListItem>
        </asp:DropDownList>
        <asp:CheckBox ID="chbShowEnabled" runat="server" AutoPostBack="True" Text="Только включенные" OnCheckedChanged="chbShowEnabled_CheckedChanged" /><br />
        <asp:GridView ID="dgvListValues" runat="server" AutoGenerateColumns="False" AllowPaging="True" OnPageIndexChanging="dgvListValues_PageIndexChanging" OnDataBound="dgvListValues_DataBound">
            <Columns>
                <asp:TemplateField HeaderText="Включено">
                    <HeaderTemplate>
                        <asp:CheckBox ID="cbSet" runat="server" Text="Все включено" AutoPostBack="True" OnCheckedChanged="cbSet_CheckedChanged"/>
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:CheckBox ID="chbEnabled" runat="server" Checked='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Enabled"))%>' />
                        <input type="hidden" runat="server" id="RowID" value='<%#DataBinder.Eval(Container, "DataItem.ID")%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderText="Значение" DataField="DisplayValue" />
            </Columns>
        </asp:GridView>    
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />&nbsp;
    </div>
</asp:Content>