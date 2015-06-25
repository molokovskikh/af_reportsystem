<%@ Page Language="C#" AutoEventWireup="true" Inherits="Reports_GeneralReports" Theme="MainWithHighLight" MasterPageFile="~/Reports/ReportMasterPage.master" Codebehind="GeneralReports.aspx.cs" %>

<asp:Content runat="server" ID="ReportGeneralReportsContent" ContentPlaceHolderID="ReportContentPlaceHolder">
	<script>
		$(document).ready(function () {
			var payerNameElement = null;
			var lastRequest = null;
			var renewPayersList = function () {
				if (lastRequest)
					lastRequest.abort();
				console.log("Отсылаем запрос для поиска имен плательщиков");
				var name = $(payerNameElement).val();
				if (!name)
					return;
				var parent = $(payerNameElement).parent();
				lastRequest =$.ajax({
					url: "../ReportsTuning/FindPayers?name=" + encodeURIComponent(name),
					type: 'POST',
					dataType: "json",
					success: function (data) {
						var obj = JSON.parse(data);
						var str = "";
						console.log(obj);
						$(parent).find(".payersList").html("");
						for (var i = 0; i < obj.payers.length; i++) {
							var id = obj.payers[i].Id;
							var name = obj.payers[i].Name;
							str += "<div style='cursor: pointer' data='" + id + "'>" + name + "</div>";
							if(obj.payers.length == 1)
								$(parent).find("input[type='hidden']").val(id);
						}
						$(parent).find(".payersList").html(str);
						$(parent).find(".payersList div").on("click",function (index, value) {
							console.log($(this));
							var data = $(this).attr("data");
							$(parent).find("input[type='hidden']").val(data);
							$(payerNameElement).val($(this).html());
							$(parent).find(".payersList").hide();
						});
						$(parent).find(".payersList").show();
					},
					error: function (event) {
						if (event.statusText == "abort")
							return;
						$(parent).find(".payersList").hide();
					}
				});
			}
			
			
			//Обнуляем счетчик, если пользователь что-то еще ввел
			$(".payerName").on("keydown", function() {
				console.log("Имя платильщика изменилось");
				payerNameElement = this;
				renewPayersList();
			});
			
		});
	</script>
	<div align="center">
		<strong style="font-size:small;">Настройка отчетов</strong><br/><br/>
		<asp:Label ID="lblMessage" runat="server" Text="" /><br/><br/>
		<asp:Label ID="lblFilter" runat="server" Text="Фильтр:" />
		<asp:TextBox ID="tbFilter" runat="server" SkinID="paramTextBoxSkin"
			ontextchanged="btnFilter_Click" ToolTip="e-mail адреса можно задавать через запятую"/>
		<asp:Button ID="btnFilter" runat="server" Text="Фильтровать"
			onclick="btnFilter_Click" />
			<br/><br/>
		<br/>
		<asp:GridView ID="dgvReports" runat="server" AutoGenerateColumns="False"  CssClass="DocumentDataTable HighLightCurrentRow"
			OnRowCommand="dgvReports_RowCommand" OnRowDeleting="dgvReports_RowDeleting"
			OnRowDataBound="dgvReports_RowDataBound" style="table-layout:fixed;"
			AllowSorting="true" onrowcreated="dgvReports_RowCreated"
			onsorting="dgvReports_Sorting" DataKeyNames="GeneralReportCode">
			<Columns>
				<asp:BoundField DataField="GeneralReportCode" HeaderText="Код"
					ItemStyle-Width="3%" HeaderStyle-Width="3%" SortExpression="GeneralReportCode">
<HeaderStyle Width="3%"></HeaderStyle>

<ItemStyle Width="3%"></ItemStyle>
				</asp:BoundField>

				<asp:TemplateField HeaderText="Биллинг код"  SortExpression="PayerID" HeaderStyle-Width="5%">
					<ItemTemplate>
						<a href='<%# String.Format("http://stat.analit.net/adm/Billing/edit.rails?BillingCode={0}", DataBinder.Eval(Container.DataItem, "PayerID")) %>'> <%# DataBinder.Eval(Container.DataItem, "PayerID") %></a>
					</ItemTemplate>
				</asp:TemplateField>

				<asp:TemplateField HeaderText="Плательщик" ItemStyle-Width="10%" HeaderStyle-Width="10%" ItemStyle-Wrap="true" SortExpression="PayerShortName">
					<ItemTemplate>
						<asp:Label ID="lblFirmName" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.PayerShortName") %>'/>
						<asp:LinkButton ID="linkEdit" runat="server" Visible="true" Style="float: right;"
							CommandName="editPayer" CommandArgument='<%# DataBinder.Eval(Container, "DataItem.GeneralReportCode") %>'>
							<asp:Image ID="imgEdit" runat="server" AlternateText="Редактировать плательщика" ImageUrl="~/Assets/Images/edit.png" />
						</asp:LinkButton>
						<div style="position:relative">
						<asp:TextBox ID="tbSearch" autocomplete="off" CssClass="payerName" runat="server" Width="79px" Visible="False"/>
						<style>
							.payersList {
								left:15px;
								 overflow-y: scroll;
								 max-height: 149px;
								 display:none;
								 background: white none repeat scroll 0 0;
								 border: 1px solid black;
								 min-width: 151px;
								 position: absolute;
							}
							.payersList div:hover { background-color: gainsboro; }
						</style>
						<div class="payersList" ></div>
						<asp:Button ID="btnSearch" runat="server" Text="Найти" OnClick="btnSearch_Click" Visible="False" />
						<asp:HiddenField ID="ddlNames" runat="server" Visible="True">
						</asp:HiddenField>
						</div>
					</ItemTemplate>

<HeaderStyle Width="10%"></HeaderStyle>

<ItemStyle Wrap="True" Width="10%"></ItemStyle>
				</asp:TemplateField>
				<asp:TemplateField HeaderText="Включен" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="Allow">
					<ItemTemplate>
						<asp:CheckBox ID="chbAllow" runat="server" Checked='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Allow")) %>' />
					</ItemTemplate>

<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:TemplateField>

				<asp:TemplateField HeaderText="Публичный" ItemStyle-Width="5%" HeaderStyle-Width="5%" SortExpression="Allow">
					<ItemTemplate>
						<asp:CheckBox ID="chbPublic" runat="server" Visible='<%# DataBinder.Eval(Container.DataItem, "PayerID") != DBNull.Value && Convert.ToInt32(DataBinder.Eval(Container.DataItem, "PayerID")) == 921 %>' Checked='<%# Convert.ToBoolean(DataBinder.Eval(Container, "DataItem.Public")) %>' />
					</ItemTemplate>

<HeaderStyle Width="5%"></HeaderStyle>

<ItemStyle Width="5%"></ItemStyle>
				</asp:TemplateField>

				<asp:TemplateField HeaderText="Примечание" SortExpression="Comment" ItemStyle-Width="45%" HeaderStyle-Width="45%">
					<ItemTemplate>
						<asp:TextBox ID="tbComment" runat="server" Text='<%# DataBinder.Eval(Container, "DataItem.Comment") %>'></asp:TextBox><br/>
					</ItemTemplate>

<HeaderStyle Width="45%"></HeaderStyle>

<ItemStyle Width="45%"></ItemStyle>
				</asp:TemplateField>
				<asp:HyperLinkField HeaderText="Рассылки" Text="..."
					DataNavigateUrlFields="GeneralReportCode"
					DataNavigateUrlFormatString="~/Contacts/Show?reportId={0}"
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
						<asp:Button ID="btApplyCopy" runat="server" Text="Применить" OnClick="btnApply_Click" Visible="false"/>
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
		<a name="addedPage"></a>
		<asp:Button ID="btnApply" runat="server" Text="Применить" OnClick="btnApply_Click" />
	</div>
</asp:Content>