<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_schedule" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Schedule.aspx.cs" %>

<asp:Content runat="server" ID="ScheduleValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">

<script type="text/javascript">
	$.noConflict();
	jQuery(document).ready(function ($) {
		$('#startDateDiv').datepicker({
			defaultDate: $('#dtFrom').val(),
			changeMonth: true,
			changeYear: true,
			onSelect: function (dateText, inst) { $('#dtFrom').val(dateText); }
		});

		$('#endDateDiv').datepicker({
			defaultDate: $('#dtTo').val(),
			changeMonth: true,
			changeYear: true,
			onSelect: function (dateText, inst) { $('#dtTo').val(dateText); }
		});
	});
</script>
	<div align="center"><strong><font size ="2">        
������� ��� ������ "<asp:Label ID="lblReportComment" runat="server" Text="Label"/>" ��� ����������� "<asp:Label ID="lblClient" runat="server" Text="Label"/>"<br /><br />
<asp:Label ID="ErrorMassage" runat="server" Text=""/>
</font></strong></div>
	<div><font size ="2">
	<table width="100%">
		<tr bgcolor="#eef8ff"><td>
			<asp:Label ID="Label2" runat="server" Text="���������:" SkinID="scheduleLabelSkin"></asp:Label>
			<asp:Label ID="lblWork" runat="server" Text="Label"></asp:Label>
		</td></tr>
		<tr bgcolor="#f6f6f6"><td>
			<asp:Label ID="Label1" runat="server" Text="������� �����:" SkinID="scheduleLabelSkin"></asp:Label>
			<asp:Label ID="lblFolder" runat="server" Text="Label"></asp:Label>
		</td></tr>
		<tr bgcolor="#eef8ff"><td>
			<asp:CheckBox ID="chbAllow" runat="server" Text="���������" 
				oncheckedchanged="chbAllow_CheckedChanged" />
		</td></tr>
	</table>
	</font>
<br />
		<br />
		<br />
		<br />
		<center><b><font size ="2"><label id="HeadLabel" >��������� ����� �� ��������� ������ � �������� �� ��������� �������</label></font></b></center>
		<br />
	</div>
	<div>
		<table cellspacing=0px style="background-color: rgb(235, 235, 235);">
			<tr>
				<td>
						<asp:Label ID="Label3" runat="server" Text="������ �������" Style="margin-left:0px;" ></asp:Label>
						<div id="startDateDiv"></div>
						<asp:HiddenField ID="dtFrom" runat="server" Visible="True" ClientIDMode="Static"/>

				</td>
				<td>
						<asp:Label ID="Label4" runat="server" Text="����� ������� (������������)" Style="margin-left:5px;"></asp:Label>
						<div id="endDateDiv"></div>
						<asp:HiddenField ID="dtTo" runat="server" Visible="True" ClientIDMode="Static"/>
				</td>
			</tr>
			<tr >
				<td valign="top" style="width:325px;" colspan=2>
						<br />
						<asp:RadioButton ID="RadioSelf" runat="server" GroupName="Mailing"
							Text="��������� � �������� ��:" Checked="True" />
						<br />
						<asp:RadioButton ID="RadioMails" runat="server" GroupName="Mailing"
							Text="��������� � ������� �� ��������� ������:" />
						<br />
						<br />
						<asp:Label ID="Label5" runat="server" Width=420px Text="�������� (������ ����� �������): " ></asp:Label>
			<br />
		  <asp:RegularExpressionValidator ID="RegularExpressionValidator1" 
   runat="server" ErrorMessage="������� ������ EMail" 
   Display="Dynamic"
	ControlToValidate="mail_Text" ValidationExpression="^\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*(\,\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*)*$" 
	ValidationGroup="mail_Text_Group"></asp:RegularExpressionValidator>

  <asp:RequiredFieldValidator ID="RequiredFieldValidator2" 
  Display="Dynamic"
   runat="server" ErrorMessage="�� ����� ������" ControlToValidate="mail_Text"
   ValidationGroup="mail_Text_Group"></asp:RequiredFieldValidator>


						<asp:TextBox ID="mail_Text" runat="server" style="background-color: white;
							 border-color:black; border-width:1px; color: black;"
						 TextMode=MultiLine Columns="50" Rows="6" runat=server></asp:TextBox>

	<asp:Label ID="Label7" runat="server" Width=419px Text="��������: (adr1@dom.com, adr2@dom.com, ... )"></asp:Label>
						 <br />
						 <br />
						 <br />
											 <asp:Button ID="btn_Mailing" runat="server" Text="���������" 
						ValidationGroup="vgPassword" OnClick="btnExecute_mailing" Width="240px" />
						
				</td>
				<td style="width: 268435488px"> 
				</td>
			</tr>
		</table>
	</div>
	<br />
	<br />
			<center><b><font size ="2"><label id="Label6" >������ ���������� ��� ������</label></font></b> <br /> <br />
				<asp:Button ID="btnExecute" runat="server" Text="��������� �������" 
			ValidationGroup="vgPassword" OnClick="btnExecute_Click" style="height: 26px" /></center>
	<br />
	<div align="center">
		<asp:GridView ID="dgvSchedule" runat="server" AutoGenerateColumns="False" Caption="����������" OnRowCommand="dgvSchedule_RowCommand" OnRowDeleting="dgvSchedule_RowDeleting" OnRowDataBound="dgvSchedule_RowDataBound">
			<Columns>
				<asp:TemplateField HeaderText="����� ������">
					<ItemTemplate>
						<asp:TextBox ID="tbStart" runat="server" ></asp:TextBox>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbMonday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SMonday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbTuesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.STuesday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbWednesday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SWednesday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbThursday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SThursday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbFriday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SFriday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbSaturday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSaturday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="��">
					<ItemTemplate>
						<asp:CheckBox ID="chbSunday" runat="server" Checked ='<%#Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSunday"))%>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="��������" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" Text="�������" CommandName="Delete" />
				</ItemTemplate>
				</asp:TemplateField>
			</Columns>
			<EmptyDataTemplate>
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="�������� ����������" />
			</EmptyDataTemplate>
		</asp:GridView>
		<br/>
		<asp:GridView ID="gvOtherTriggers" runat="server" 
			Caption="�������������� ����������" AutoGenerateColumns="False" >
			<Columns>
				<asp:BoundField DataField="!" HeaderText="��������" />
			</Columns>
		</asp:GridView>
		<br/>
		<div align="center" style="width:70%;">
		<asp:GridView ID="gvLogs" runat="server" 
			Caption="���������� ���������� ������" AutoGenerateColumns="False"  EmptyDataText="��� ������">
			<Columns>
				<asp:BoundField DataField="LogTime" HeaderText="����" />
				<asp:BoundField DataField="EMail" HeaderText="EMail" />
				<asp:BoundField DataField="SMTPID" HeaderText="SMTPID" />
			</Columns>
		</asp:GridView>
		</div>
		<br/>
		<asp:Button ID="btnApply" runat="server" Text="���������" OnClick="btnApply_Click" ValidationGroup="vgPassword" />
	</div>
</asp:Content>