<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_GeneralReports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="GeneralReports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong><br/>
<!--                
                <asp:TemplateField HeaderText="Имя файла отчета" ItemStyle-Width="5%" HeaderStyle-Width="5%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbFile" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRFileName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Имя архива отчета" ItemStyle-Width="5%" HeaderStyle-Width="5%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbArch" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRArchName")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                ItemStyle-Width="35%" HeaderStyle-Width="35%" 
                -->
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" 
            OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" 
            OnRowDataBound="dgvReports_RowDataBound" style="table-layout:fixed;" 
            AllowSorting="true" onsorting="dgvReports_Sorting">
            <Columns>
                <asp:BoundField DataField="GRCode" HeaderText="Код" ItemStyle-Width="3%" HeaderStyle-Width="3%" SortExpression="GRCode"/>
                <asp:HyperLinkField DataTextField="PayerID" DataNavigateUrlFields="GRFirmCode" HeaderText="Биллинг код" DataNavigateUrlFormatString="https://stat.analit.net/adm/Billing/edit.rails?ClientCode={0}" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="PayerID"/>
                <asp:TemplateField HeaderText="Плательщик" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-Wrap="true" SortExpression="GRPayerShortName">
                    <ItemTemplate>
                        <asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.GRPayerShortName") %>'/>
                        <asp:TextBox ID="tbSearch" runat="server" Width="79px" Visible="False"/>
                        <asp:Button ID="btnSearch" runat="server" Text="Найти" OnClick="btnSearch_Click" Visible="False" />
                        <asp:DropDownList ID="ddlNames" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Включен" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="GRAllow">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbAllow" runat="server" Checked='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.GRAllow"))%>' />
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Комментарий" SortExpression="GRComment">
                    <ItemTemplate>
                        <asp:TextBox ID="tbComment" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.GRComment")%>'></asp:TextBox>
                    </ItemTemplate>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Рассылки" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Contacts.aspx?GeneralReport={0}" ItemStyle-Width="5%" HeaderStyle-Width="5%"/>
                <asp:HyperLinkField HeaderText="Отчеты" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Reports.aspx?r={0}" ItemStyle-Width="5%" HeaderStyle-Width="5%"/>
                <asp:HyperLinkField HeaderText="Расписание" Text="..." DataNavigateUrlFields="GRCode" DataNavigateUrlFormatString="Schedule.aspx?r={0}" ItemStyle-Width="5%" HeaderStyle-Width="6%"/>
                <asp:TemplateField ItemStyle-Width="6%" HeaderStyle-Width="6%">
				    <HeaderTemplate>
					    <asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				    </HeaderTemplate>
				    <ItemTemplate>
					    <asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				    </ItemTemplate>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить отчет" />
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
</asp:Content>