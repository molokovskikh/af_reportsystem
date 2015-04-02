<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_schedule" Theme="Main" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="Schedule.aspx.cs" %>

<asp:Content runat="server" ID="ScheduleValuesContent" ContentPlaceHolderID="ReportContentPlaceHolder">

	<script type="text/javascript">
		jQuery(document).ready(function ($) {
			var reportScheduleFromDate = GetCookie("ReportScheduleFromDate");
			var reportScheduleToDate = GetCookie("ReportScheduleToDate");
			console.log("Checking cookie dates: ", reportScheduleFromDate, reportScheduleToDate);
			if (reportScheduleFromDate && reportScheduleToDate) {
				$('#dtFrom').val(reportScheduleFromDate);
				$('#dtTo').val(reportScheduleToDate);
			}

			$('#startDateDiv').datepicker({
				defaultDate: $('#dtFrom').val(),
				changeMonth: true,
				changeYear: true,
				onSelect: function (dateText, inst) {
					$('#dtFrom').val(dateText);
					SetCookie("ReportScheduleFromDate", dateText);
				}
			});

			$('#endDateDiv').datepicker({
				defaultDate: $('#dtTo').val(),
				changeMonth: true,
				changeYear: true,
				onSelect: function(dateText, inst) {
					$('#dtTo').val(dateText);
					SetCookie("ReportScheduleToDate", dateText);
				}
			});

			//проверим-ка мы состояние страницы
			//запрос отправляется на ту же самую страницу до тех пор, пока в коде ответа в том месте где лежит сообщение
			//мы не увидим, что нас ожидал успех - тогда мы его отображаем и расслабляемся.
			var busy = false;
			var interval = setInterval(function () {
				console.log("interval")
				var msg = $(".error").html();
				//отправляем 1 запрос за раз только в том случае, если на странице начался запуск отчета, о чем нам скажет сообщение
				//хотя иногда там уже сразу написано "Операция выполнена", тогда ничего и делать не надо.
				if (msg != "" && !busy) {
					console.log("Обновляем данные");
					busy = true;
					$.ajax({
						url: document.location.href,
					}).done(function (responseText) {
						//Находим отображаемое сообщение
						var regex = /(<form[\s\S]*<\/form>)/g;
						var matches = regex.exec(responseText);
						console.log(matches);
						if (matches != null) {
							var newbody = $(matches[0]);
							console.log(newbody.get(0));
							//Отображаем обновленное сообщение
							$(".error").html(newbody.find(".error").html());
							$(".error").attr("style", newbody.find(".error").attr("style"));
							//Если операция выполнена, то расслабляемся и останавливаем выполнение
							if (newbody.html() == "" || newbody.find(".error").html().indexOf("Операция выполнена") >= 0) {
								console.log("Операция выполнена");
								clearInterval(interval);
								$(".executeMailing").removeAttr('disabled');
								$(".execute").removeAttr('disabled');

								//Обновляем статистику
								var reportSend = newbody.find(".reportSendStatistic").html();
								if (reportSend)
									$(".reportSendStatistic").parent().html(reportSend);
								var reportRun = newbody.find(".reportRunStatistic").html();
								if(reportRun)
									$(".reportRunStatistic").parent().html(reportRun);
							}
						} else {
							//Иногда просто раз - и все: на новой странице нет никакого дополнительного сообщения. Что в этом случае делать непонятно.
							console.log("Сбой");
							console.log(responseText);
							clearInterval(interval);
							$(".executeMailing").removeAttr('disabled');
							$(".execute").removeAttr('disabled');
							$(".error").html("");
						}
					}).always(function() {
						busy = false;
					});
				}
			},500);
		});
</script>
	<div align="center"><strong><font size ="2">
Задание для отчета "<asp:Label ID="lblReportComment" runat="server" Text="Label"/>" для плательщика "<asp:Label ID="lblClient" runat="server" Text="Label"/>"<br /><br />
<asp:Label ID="ErrorMassage" runat="server" Text=""/>
</font></strong></div>
	<div><font size ="2">
	<table width="100%">
		<tr bgcolor="#eef8ff"><td>
			<asp:Label ID="Label2" runat="server" Text="Выполнить:" SkinID="scheduleLabelSkin"></asp:Label>
			<asp:Label ID="lblWork" runat="server" Text="Label"></asp:Label>
		</td></tr>
		<tr bgcolor="#f6f6f6"><td>
			<asp:Label ID="Label1" runat="server" Text="Рабочая папка:" SkinID="scheduleLabelSkin"></asp:Label>
			<asp:Label ID="lblFolder" runat="server" Text="Label"></asp:Label>
		</td></tr>
		<tr bgcolor="#eef8ff"><td>
			<asp:CheckBox ID="chbAllow" runat="server" Text="Разрешено"
				oncheckedchanged="chbAllow_CheckedChanged" />
		</td></tr>
	</table>
	</font>
<br />
		<br />
		<br />
		<br />
		<center><b><font size ="2"><label id="HeadLabel" >Выполнить отчет за указанный период и отослать по выбранным адресам</label></font></b></center>
		<br />
	</div>
	<div>
		<table cellspacing=0px style="background-color: rgb(235, 235, 235);">
			<tr>
				<td>
						<asp:Label ID="Label3" runat="server" Text="Начало периода" Style="margin-left:0px;" ></asp:Label>
						<div id="startDateDiv"></div>
						<asp:HiddenField ID="dtFrom" runat="server" Visible="True" ClientIDMode="Static"/>

				</td>
				<td>
						<asp:Label ID="Label4" runat="server" Text="Конец периода (включительно)" Style="margin-left:5px;"></asp:Label>
						<div id="endDateDiv"></div>
						<asp:HiddenField ID="dtTo" runat="server" Visible="True" ClientIDMode="Static"/>
				</td>
			</tr>
			<tr >
				<td valign="top" style="width:325px;" colspan=2>
						<br />
						<asp:RadioButton ID="RadioSelf" runat="server" GroupName="Mailing"
							Text="Выполнить и отослать на:" Checked="True" />
						<br />
						<asp:RadioButton ID="RadioMails" runat="server" GroupName="Mailing"
							Text="Выполнить и выслать на указанные адреса:" />
						<br />
						<br />
						<asp:Label ID="Label5" runat="server" Width=420px Text="Адресаты (писать через запятую): " ></asp:Label>
			<br />
		  <asp:RegularExpressionValidator ID="RegularExpressionValidator1"
   runat="server" ErrorMessage="Введите список EMail"
   Display="Dynamic"
	ControlToValidate="mail_Text" ValidationExpression="^\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*(\,\s*\w[\w\.\-]*[@]\w[\w\.\-]*([.]([\w]{1,})){1,3}\s*)*$"
	ValidationGroup="mail_Text_Group"></asp:RegularExpressionValidator>

  <asp:RequiredFieldValidator ID="RequiredFieldValidator2"
  Display="Dynamic"
   runat="server" ErrorMessage="Не задан список" ControlToValidate="mail_Text"
   ValidationGroup="mail_Text_Group"></asp:RequiredFieldValidator>


						<asp:TextBox ClientIDMode="Static" ID="mail_Text" runat="server" style="background-color: white;
							 border-color:black; border-width:1px; color: black;"
						 TextMode=MultiLine Columns="50" Rows="6" runat=server></asp:TextBox>

	<asp:Label ID="Label7" runat="server" Width=419px Text="Например: (adr1@dom.com, adr2@dom.com, ... )"></asp:Label>
						<br />
						<br />
						<br />
						<asp:Button ID="btn_Mailing" CssClass="executeMailing" runat="server" Text="Выполнить" ValidationGroup="vgPassword" OnClick="btnExecute_mailing" Width="200px" />
						<asp:Button ID="send_created_report" runat="server" Text="Выслать готовый" ValidationGroup="vgPassword" OnClick="btnExecute_sendReady" Width="200px" />

				</td>
				<td style="width: 268435488px">
				</td>
			</tr>
		</table>
	</div>
	<br />
	<br />
			<center><b><font size ="2"><label id="Label6" >Задать расписание для отчета</label></font></b> <br /> <br />
				<asp:Button ID="btnExecute" runat="server" CssClass="execute" Text="Выполнить задание"
			ValidationGroup="vgPassword" OnClick="btnExecute_Click" style="height: 26px" /></center>

	<div align="center" id="sheduleSettings">

		<asp:GridView ID="dgvSchedule" runat="server" AutoGenerateColumns="False"
			Caption="Еденедельное расписание" OnRowCommand="dgvSchedule_RowCommand"
			OnRowDeleting="dgvSchedule_RowDeleting"
			OnRowDataBound="dgvSchedule_RowDataBound">
			<Columns>
				<asp:TemplateField HeaderText="Время начала">
					<ItemTemplate>
						<asp:TextBox ID="tbStart" runat="server" ></asp:TextBox>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Пн">
					<ItemTemplate>
						<asp:CheckBox ID="chbMonday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SMonday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Вт">
					<ItemTemplate>
						<asp:CheckBox ID="chbTuesday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.STuesday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Ср">
					<ItemTemplate>
						<asp:CheckBox ID="chbWednesday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SWednesday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Чт">
					<ItemTemplate>
						<asp:CheckBox ID="chbThursday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SThursday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Пт">
					<ItemTemplate>
						<asp:CheckBox ID="chbFriday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SFriday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Сб">
					<ItemTemplate>
						<asp:CheckBox ID="chbSaturday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSaturday")) %>' />
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Вс">
					<ItemTemplate>
						<asp:CheckBox ID="chbSunday" runat="server" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.SSunday")) %>' />
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
				<asp:Button ID="btnAdd" runat="server" CommandName="Add" Text="Добавить еженедельное расписание" />
			</EmptyDataTemplate>
		</asp:GridView>

		<asp:GridView ID="dgvScheduleMonth" runat="server"
			AutoGenerateColumns="False" Caption="Ежемесячное расписание"
			onrowcommand="dgvScheduleMonth_RowCommand"
			OnRowDataBound="dgvSchedule_RowDataBoundMonth"
			OnRowDeleting="dgvScheduleMonth_RowDeleting">
				<Columns>
				<asp:TemplateField HeaderText="Время начала">
					<ItemTemplate>
						<asp:TextBox ID="tbStart" runat="server" ></asp:TextBox>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Месяц">
					<ItemTemplate>
						<div id="firstSixMonth">
						<asp:CheckBox ID="m1" runat="server" Text="Январь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m1")) %>' />
						<asp:CheckBox ID="m2" runat="server" Text="Февраль" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m2")) %>' />
						<asp:CheckBox ID="m3" runat="server" Text="Март" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m3")) %>' />
						<asp:CheckBox ID="m4" runat="server" Text="Апрель" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m4")) %>' />
						<asp:CheckBox ID="m5" runat="server" Text="Май" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m5")) %>' />
						<asp:CheckBox ID="m6" runat="server" Text="Июнь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m6")) %>' />
						</div>
						<div id="secondSixMonth">
						<asp:CheckBox ID="m7" runat="server" Text="Июль" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m7")) %>' />
						<asp:CheckBox ID="m8" runat="server" Text="Август" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m8")) %>' />
						<asp:CheckBox ID="m9" runat="server" Text="Сентябрь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m9")) %>' />
						<asp:CheckBox ID="m10" runat="server" Text="Октябрь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m10")) %>' />
						<asp:CheckBox ID="m11" runat="server" Text="Ноябрь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m11")) %>' />
						<asp:CheckBox ID="m12" runat="server" Text="Декабрь" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.m12")) %>' />
						</div>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Дни">
					<ItemTemplate>
						<div id="firstFifteenDays">
						<asp:CheckBox ID="d1" runat="server" Text="1" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d1")) %>' />
						<asp:CheckBox ID="d2" runat="server" Text="2" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d2")) %>' />
						<asp:CheckBox ID="d3" runat="server" Text="3" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d3")) %>' />
						<asp:CheckBox ID="d4" runat="server" Text="4" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d4")) %>' />
						<asp:CheckBox ID="d5" runat="server" Text="5" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d5")) %>' />
						<asp:CheckBox ID="d6" runat="server" Text="6" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d6")) %>' />
						<asp:CheckBox ID="d7" runat="server" Text="7" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d7")) %>' />
						<asp:CheckBox ID="d8" runat="server" Text="8" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d8")) %>' />
						<asp:CheckBox ID="d9" runat="server" Text="9" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d9")) %>' />
						<asp:CheckBox ID="d10" runat="server" Text="10" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d10")) %>' />
						<asp:CheckBox ID="d11" runat="server" Text="11" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d11")) %>' />
						<asp:CheckBox ID="d12" runat="server" Text="12" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d12")) %>' />
						<asp:CheckBox ID="d13" runat="server" Text="13" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d13")) %>' />
						<asp:CheckBox ID="d14" runat="server" Text="14" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d14")) %>' />
						<asp:CheckBox ID="d15" runat="server" Text="15" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d15")) %>' />
						</div>
						<div id="secondFifteenDays">
						<asp:CheckBox ID="d16" runat="server" Text="16" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d16")) %>' />
						<asp:CheckBox ID="d17" runat="server" Text="17" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d17")) %>' />
						<asp:CheckBox ID="d18" runat="server" Text="18" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d18")) %>' />
						<asp:CheckBox ID="d19" runat="server" Text="19" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d19")) %>' />
						<asp:CheckBox ID="d20" runat="server" Text="20" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d20")) %>' />
						<asp:CheckBox ID="d21" runat="server" Text="21" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d21")) %>' />
						<asp:CheckBox ID="d22" runat="server" Text="22" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d22")) %>' />
						<asp:CheckBox ID="d23" runat="server" Text="23" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d23")) %>' />
						<asp:CheckBox ID="d24" runat="server" Text="24" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d24")) %>' />
						<asp:CheckBox ID="d25" runat="server" Text="25" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d25")) %>' />
						<asp:CheckBox ID="d26" runat="server" Text="26" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d26")) %>' />
						<asp:CheckBox ID="d27" runat="server" Text="27" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d27")) %>' />
						<asp:CheckBox ID="d28" runat="server" Text="28" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d28")) %>' />
						<asp:CheckBox ID="d29" runat="server" Text="29" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d29")) %>' />
						<asp:CheckBox ID="d30" runat="server" Text="30" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d30")) %>' />
						<asp:CheckBox ID="d31" runat="server" Text="31" Checked ='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.d31")) %>' />
						</div>
					</ItemTemplate>
				</asp:TemplateField>
				<asp:TemplateField>
				<HeaderTemplate>
					<asp:Button ID="btnAdd" runat="server" Text="Добавить" CommandName="Add" />
				</HeaderTemplate>
				<ItemTemplate>
					<asp:Button ID="btnDelete" runat="server" CssClass="deleteMonthItem" Text="Удалить" CommandName="Delete" />
				</ItemTemplate>
				</asp:TemplateField>
			</Columns>
			<EmptyDataTemplate>
				<asp:Button ID="btnAdd"  CssClass="addMonthItem" runat="server" CommandName="Add" Text="Добавить ежемесячное расписание" />
			</EmptyDataTemplate>
		</asp:GridView>

		<asp:GridView ID="gvOtherTriggers" runat="server"
			Caption="Дополнительное расписание" AutoGenerateColumns="False" >
			<Columns>
				<asp:BoundField DataField="!" HeaderText="Описание" />
			</Columns>
		</asp:GridView>
		<br/>
		<div align="center" class="midleWidth">
		<asp:GridView ID="startLogs" runat="server" CssClass="reportRunStatistic"
			Caption="Статистика запусков отчета" AutoGenerateColumns="False"  EmptyDataText="Нет данных">
			<Columns>
				<asp:BoundField DataField="StartTime" HeaderText="Время запуска" />
				<asp:BoundField DataField="EndTime" HeaderText="Время заверщения" />
			</Columns>
		</asp:GridView>
		</div>
		<br/>
		<div align="center" class="midleWidth">
		<asp:GridView ID="gvLogs" runat="server" CssClass="reportSendStatistic"
			Caption="Статистика отсылки отчета" AutoGenerateColumns="False"  EmptyDataText="Нет данных">
			<Columns>
				<asp:BoundField DataField="LogTime" HeaderText="Дата" />
				<asp:BoundField DataField="EMail" HeaderText="EMail" />
				<asp:BoundField DataField="SMTPID" HeaderText="SMTPID" />
			</Columns>
		</asp:GridView>
		</div>
		<br/>
		<asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" ValidationGroup="vgPassword" />
	</div>
</asp:Content>