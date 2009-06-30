<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_GeneralReports" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="GeneralReports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
    <div align="center">
        <strong style="font-size:small;">Настройка отчетов</strong><br/><br/>
        <asp:Label ID="lblFilter" runat="server" Text="Фильтр:" />
        <asp:TextBox ID="tbFilter" runat="server" SkinID="paramTextBoxSkin" 
            ontextchanged="btnFilter_Click"/>
        <asp:Button ID="btnFilter" runat="server" Text="Фильтровать" 
            onclick="btnFilter_Click" /><br/><br/>
        <br/>
        <asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False" 
            OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" 
            OnRowDataBound="dgvReports_RowDataBound" style="table-layout:fixed;" 
            AllowSorting="true" onrowcreated="dgvReports_RowCreated" onsorting="dgvReports_Sorting" DataKeyNames="GeneralReportCode">
            <Columns>
                <asp:BoundField DataField="GeneralReportCode" HeaderText="Код" 
                    ItemStyle-Width="3%" HeaderStyle-Width="3%" SortExpression="GeneralReportCode">
<HeaderStyle Width="3%"></HeaderStyle>

<ItemStyle Width="3%"></ItemStyle>
                </asp:BoundField>
                <asp:HyperLinkField DataTextField="PayerID" DataNavigateUrlFields="FirmCode" 
                    HeaderText="Биллинг код" 
                    DataNavigateUrlFormatString="https://stat.analit.net/adm/Billing/edit.rails?ClientCode={0}" 
                    ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="PayerID">
<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
                </asp:HyperLinkField>
                <asp:TemplateField HeaderText="Плательщик" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-Wrap="true" SortExpression="PayerShortName">
                    <ItemTemplate>
                        <asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PayerShortName") %>'/>
                        <asp:TextBox ID="tbSearch" runat="server" Width="79px" Visible="False"/>
                        <asp:Button ID="btnSearch" runat="server" Text="Найти" OnClick="btnSearch_Click" Visible="False" />
                        <asp:DropDownList ID="ddlNames" runat="server" Visible="False">
                        </asp:DropDownList>
                    </ItemTemplate>

<HeaderStyle Width="10%"></HeaderStyle>

<ItemStyle Wrap="True" Width="10%"></ItemStyle>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Включен" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="Allow">
                    <ItemTemplate>
                        <asp:CheckBox ID="chbAllow" runat="server" Checked='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Allow"))%>' />
                    </ItemTemplate>

<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
                </asp:TemplateField>
                <asp:TemplateField HeaderText="Примечание" SortExpression="Comment" ItemStyle-Width="45%" HeaderStyle-Width="45%">
                    <ItemTemplate>
                        <asp:TextBox ID="tbComment" runat="server" Text='<%#DataBinder.Eval(Container, "DataItem.Comment")%>'></asp:TextBox>
                    </ItemTemplate>

<HeaderStyle Width="45%"></HeaderStyle>

<ItemStyle Width="45%"></ItemStyle>
                </asp:TemplateField>
                <asp:HyperLinkField HeaderText="Рассылки" Text="..." 
                    DataNavigateUrlFields="GeneralReportCode" 
                    DataNavigateUrlFormatString="Contacts.aspx?GeneralReport={0}" 
                    ItemStyle-Width="5%" HeaderStyle-Width="5%">
<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
                </asp:HyperLinkField>
                <asp:HyperLinkField HeaderText="Отчеты" Text="..." 
                    DataNavigateUrlFields="GeneralReportCode" 
                    DataNavigateUrlFormatString="Reports.aspx?r={0}" ItemStyle-Width="5%" 
                    HeaderStyle-Width="5%">
<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
                </asp:HyperLinkField>
                <asp:HyperLinkField HeaderText="Расписание" Text="..."
                    DataNavigateUrlFields="GeneralReportCode"                     
                    DataNavigateUrlFormatString="Schedule.aspx?r={0}" ItemStyle-Width="6%" 
                    HeaderStyle-Width="6%">
<HeaderStyle Width="6%"></HeaderStyle>

<ItemStyle Width="6%" ></ItemStyle>
                </asp:HyperLinkField>
                <asp:TemplateField ItemStyle-Width="7%" HeaderStyle-Width="7%">
				    <HeaderTemplate>
					    <asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				    </HeaderTemplate>
				    <ItemTemplate>
					    <asp:Button ID="btnDelete" runat="server" Text="Удалить" CommandName="Delete" />
				    </ItemTemplate>

<HeaderStyle Width="7%"></HeaderStyle>

<ItemStyle Width="7%"></ItemStyle>
                </asp:TemplateField>
            </Columns>
   			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить отчет"/>
			</EmptyDataTemplate>
        </asp:GridView>
        <asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
    </div>
</asp:Content>