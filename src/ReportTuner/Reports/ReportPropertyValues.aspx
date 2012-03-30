<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_ReportPropertyValues" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="ReportPropertyValues.aspx.cs" %>

<asp:Content runat="server" ID="ReportPropertyValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
            <strong style="font-size:small;">�������������� �������� ������ "<asp:Label ID="lblListName" runat="server" Text="Label"/>" ������ "<asp:Label ID="lblReportCaption" runat="server" Text="Label"/>" ���� ������ "<asp:Label ID="lblReportType" runat="server" Text="Label"/>"</strong><br/>
        <asp:TextBox ID="tbSearch" runat="server" OnTextChanged="tbSearch_TextChanged"></asp:TextBox>
        <asp:Button ID="btnSearch" runat="server" OnClick="btnSearch_Click" Text="�����" /><br />
        &nbsp; &nbsp;&nbsp;
        <asp:Label ID="Label1" runat="server" Text="�� ��������:"></asp:Label><asp:DropDownList
            ID="ddlPages" runat="server" AutoPostBack="True" OnSelectedIndexChanged="ddlPages_SelectedIndexChanged">
            <asp:ListItem>10</asp:ListItem>
            <asp:ListItem>20</asp:ListItem>
            <asp:ListItem>50</asp:ListItem>
            <asp:ListItem>100</asp:ListItem>
        </asp:DropDownList>
        <asp:CheckBox ID="chbShowEnabled" runat="server" AutoPostBack="True" Text="������ ����������" OnCheckedChanged="chbShowEnabled_CheckedChanged" /><br />
		<div style="width: 600px;" align="right">
        <asp:GridView ID="dgvListValues" runat="server" AutoGenerateColumns="False" 
				AllowPaging="True" OnPageIndexChanging="dgvListValues_PageIndexChanging" 
				OnDataBound="dgvListValues_DataBound" ViewStateMode="Enabled">
            <Columns>
                <asp:TemplateField HeaderText="��������" ItemStyle-HorizontalAlign="Right" HeaderStyle-HorizontalAlign="Right">
                    <HeaderTemplate>
                        <asp:CheckBox ID="cbSet" runat="server" Text="��� ��������" AutoPostBack="True" OnCheckedChanged="cbSet_CheckedChanged" TextAlign="Left" />
                    </HeaderTemplate>
                    <ItemTemplate>
                        <asp:CheckBox ID="chbEnabled" runat="server" Checked='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Enabled"))%>'/>
                        <input type="hidden" runat="server" id="RowID" value='<%#DataBinder.Eval(Container, "DataItem.ID")%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:BoundField HeaderText="��������" DataField="DisplayValue" ItemStyle-HorizontalAlign="Left" />
            </Columns>
        </asp:GridView> 
		</div>   
        <asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" 
				style="height: 26px" />&nbsp;
    </div>
</asp:Content>