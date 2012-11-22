<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_GeneralReports" Theme="MainWithHighLight" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="GeneralReports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
	<div align="center">
		<strong style="font-size:small;">��������� �������</strong><br/><br/>
		<asp:Label ID="lblMessage" runat="server" Text="" /><br/><br/>
		<asp:Label ID="lblFilter" runat="server" Text="������:" />
		<asp:TextBox ID="tbFilter" runat="server" SkinID="paramTextBoxSkin" 
			ontextchanged="btnFilter_Click" ToolTip="e-mail ������ ����� �������� ����� �������"/>
		<asp:Button ID="btnFilter" runat="server" Text="�����������" 
			onclick="btnFilter_Click" />
			<br/><br/>
		<br/>
		<asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False"  CssClass="DocumentDataTable HighLightCurrentRow"
			OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting" 
			OnRowDataBound="dgvReports_RowDataBound" style="table-layout:fixed;" 
			AllowSorting="true" onrowcreated="dgvReports_RowCreated" 
			onsorting="dgvReports_Sorting" DataKeyNames="GeneralReportCode">	
			<Columns>
				<asp:BoundField DataField="GeneralReportCode" HeaderText="���" 
					ItemStyle-Width="3%" HeaderStyle-Width="3%" SortExpression="GeneralReportCode">
<HeaderStyle Width="3%"></HeaderStyle>

<ItemStyle Width="3%"></ItemStyle>
				</asp:BoundField>
				
				<asp:TemplateField HeaderText="������� ���"  SortExpression="PayerID" HeaderStyle-Width="5%">
					<ItemTemplate>
						<a href='<%# String.Format("http://stat.analit.net/adm/Billing/edit.rails?BillingCode={0}", DataBinder.Eval(Container.DataItem, "PayerID")) %>'> <%# DataBinder.Eval(Container.DataItem, "PayerID") %></a>
					</ItemTemplate>
				</asp:TemplateField>
				
				<asp:TemplateField HeaderText="����������" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-Wrap="true" SortExpression="PayerShortName">
					<ItemTemplate>
						<asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PayerShortName") %>'/>
						<asp:LinkButton ID="linkEdit" runat="server" Visible="true" Style="float: right;" 
							CommandName="editPayer" CommandArgument='<%# DataBinder.Eval(Container, "DataItem.GeneralReportCode") %>'>
							<asp:Image ID="imgEdit" runat="server" AlternateText="������������� �����������" ImageUrl="~/Images/edit.png" />
						</asp:LinkButton>
						<asp:TextBox ID="tbSearch" runat="server" Width="79px" Visible="False"/>
						<asp:Button ID="btnSearch" runat="server" Text="�����" OnClick="btnSearch_Click" Visible="False" />
						<asp:DropDownList ID="ddlNames" runat="server" Visible="False">
						</asp:DropDownList>
					</ItemTemplate>

<HeaderStyle Width="10%"></HeaderStyle>

<ItemStyle Wrap="True" Width="10%"></ItemStyle>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="�������" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="Allow">
					<ItemTemplate>
						<asp:CheckBox ID="chbAllow" runat="server" Checked='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Allow")) %>' />
					</ItemTemplate>

<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:TemplateField>

				<asp:TemplateField HeaderText="���������" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="Allow">
					<ItemTemplate>
						<asp:CheckBox ID="chbPublic" runat="server" Enabled='<%# Convert.ToInt32(DataBinder.Eval(Container.DataItem, "PayerID")) == 921 %>' Checked='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Public")) %>' />
					</ItemTemplate>

<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:TemplateField>

				<asp:TemplateField HeaderText="����������" SortExpression="Comment" ItemStyle-Width="45%" HeaderStyle-Width="45%">
					<ItemTemplate>
						<asp:TextBox ID="tbComment" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.Comment") %>'></asp:TextBox><br/>
					</ItemTemplate>

<HeaderStyle Width="45%"></HeaderStyle>

<ItemStyle Width="45%"></ItemStyle>
				</asp:TemplateField>
				<asp:HyperLinkField HeaderText="��������" Text="..." 
					DataNavigateUrlFields="GeneralReportCode" 
					DataNavigateUrlFormatString="Contacts.aspx?GeneralReport={0}" 
					ItemStyle-Width="5%" HeaderStyle-Width="5%">
<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:HyperLinkField>
				<asp:HyperLinkField HeaderText="������" Text="..." 
					DataNavigateUrlFields="GeneralReportCode" 
					DataNavigateUrlFormatString="Reports.aspx?r={0}" ItemStyle-Width="5%" 
					HeaderStyle-Width="5%">
<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:HyperLinkField>
				<asp:HyperLinkField HeaderText="����������" Text="..."
					DataNavigateUrlFields="GeneralReportCode"                     
					DataNavigateUrlFormatString="Schedule.aspx?r={0}" ItemStyle-Width="6%" 
					HeaderStyle-Width="6%">
<HeaderStyle Width="6%"></HeaderStyle>

<ItemStyle Width="6%" ></ItemStyle>
				</asp:HyperLinkField>
				<asp:TemplateField ItemStyle-Width="7%" HeaderStyle-Width="7%">
					<HeaderTemplate>
						<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
					</HeaderTemplate>
					<ItemTemplate>
						<asp:Button ID="btApplyCopy" runat="server" Text="���������" OnClick="btnApply_Click" Visible="false"/>
						<asp:Button ID="btnDelete" runat="server" Text="�������" CommandName="Delete" />
					</ItemTemplate>

<HeaderStyle Width="7%"></HeaderStyle>

<ItemStyle Width="7%"></ItemStyle>
				</asp:TemplateField>
			</Columns>
			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� �����"/>
			</EmptyDataTemplate>
		</asp:GridView>
		<a name="addedPage"></a>
		<asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" />
	</div>
</asp:Content>