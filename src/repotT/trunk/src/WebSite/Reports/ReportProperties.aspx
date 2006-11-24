<%@ Page Language="C#" AutoEventWireup="true" CodeFile="ReportProperties.aspx.cs" Inherits="Reports_ReportProperties" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" %>

<asp:Content runat="server" ID="ReportPropertiesContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка параметров отчета "<asp:Label ID="lblReport" runat="server"
            Text="Label"></asp:Label>"</strong><br/>
        <asp:GridView ID="dgvNonOptional" runat="server" AutoGenerateColumns="False" OnRowDataBound="dgvNonOptional_RowDataBound" OnRowCommand="dgvNonOptional_RowCommand" Caption="Не опциональные">
            <Columns>
                <asp:BoundField DataField="PParamName" HeaderText="Наименование параметра" />
                <asp:TemplateField HeaderText="Значение">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbValue" runat="server" Visible="False" />
                        <asp:TextBox ID="tbValue" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.PPropertyValue")%>'></asp:TextBox>
                        <asp:TextBox ID="tbSearch"  SkinID="searchTexBoxSkin" runat="server" Width="30%"></asp:TextBox>
                        <asp:Button ID="btnFind" runat="server" CommandName="Find" Text="Найти" />
                        <asp:DropDownList ID="ddlValue" runat="server" Visible="False" AutoPostBack="True" OnSelectedIndexChanged="ddlValue_SelectedIndexChanged"></asp:DropDownList>
                        <asp:Label ID="lblType" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PPropertyType") %>'  Visible="False"></asp:Label>
                        <asp:Button ID="btnListValue" runat="server" Text="..." CommandName="ShowValues" />
                    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить параметр" />
			</EmptyDataTemplate>
        </asp:GridView><asp:GridView ID="dgvOptional" runat="server" AutoGenerateColumns="False" OnRowDataBound="dgvOptional_RowDataBound" OnRowCommand="dgvOptional_RowCommand" Caption="Опциональные" OnRowDeleting="dgvOptional_RowDeleting">
            <Columns>
                <asp:TemplateField HeaderText="Наименование параметра">
                    <ItemTemplate>
                        <asp:Label ID="lblName" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.OPParamName")%>'></asp:Label>
                        <asp:DropDownList ID="ddlName" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Значение">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbValue" runat="server" Visible="False" />
                        <asp:TextBox ID="tbValue" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.OPPropertyValue")%>' Visible="False"></asp:TextBox>
                        <asp:TextBox ID="tbSearch" runat="server" SkinID="searchTexBoxSkin" Width="30%" Visible="False"></asp:TextBox>
                        <asp:Button ID="btnFind" runat="server" CommandName="Find" Text="Найти" Visible="False" />
                        <asp:DropDownList ID="ddlValue" runat="server" Visible="False" AutoPostBack="True" OnSelectedIndexChanged="ddlValue_SelectedIndexChanged">
                        </asp:DropDownList>
                        <asp:Label ID="lblType" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.OPPropertyType") %>'
                            Visible="False"></asp:Label>
                        <asp:Button ID="btnListValue" runat="server" Text="..." CommandName="ShowValues" Visible="False" />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				</ItemTemplate>
                </asp:TemplateField>
            </Columns>
            <EmptyDataTemplate>
                <asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить параметр" />
            </EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
</asp:Content>