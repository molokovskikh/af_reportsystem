using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MySql.Data.MySqlClient;
using Inforoom.ReportSystem.RatingReports;
using System.Configuration;
using System.Net.Mail;
using Inforoom.Common;
using ExecuteTemplate;
using Inforoom.ReportSystem.Properties;

namespace Inforoom.ReportSystem
{
    /// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class frmMain : System.Windows.Forms.Form
	{
		private System.Windows.Forms.TextBox tbReport;
		private System.Windows.Forms.Button btnStart;
		private System.Windows.Forms.DataGrid dgMain;
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		public frmMain()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			if( disposing )
			{
				if (components != null) 
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.tbReport = new System.Windows.Forms.TextBox();
			this.btnStart = new System.Windows.Forms.Button();
			this.dgMain = new System.Windows.Forms.DataGrid();
			((System.ComponentModel.ISupportInitialize)(this.dgMain)).BeginInit();
			this.SuspendLayout();
			// 
			// tbReport
			// 
			this.tbReport.Location = new System.Drawing.Point(16, 16);
			this.tbReport.Name = "tbReport";
			this.tbReport.Size = new System.Drawing.Size(100, 20);
			this.tbReport.TabIndex = 0;
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(136, 16);
			this.btnStart.Name = "btnStart";
			this.btnStart.Size = new System.Drawing.Size(75, 23);
			this.btnStart.TabIndex = 1;
			this.btnStart.Text = "Start";
			this.btnStart.Click += new System.EventHandler(this.btnStart_Click);
			// 
			// dgMain
			// 
			this.dgMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
						| System.Windows.Forms.AnchorStyles.Left)
						| System.Windows.Forms.AnchorStyles.Right)));
			this.dgMain.DataMember = "";
			this.dgMain.HeaderForeColor = System.Drawing.SystemColors.ControlText;
			this.dgMain.Location = new System.Drawing.Point(8, 56);
			this.dgMain.Name = "dgMain";
			this.dgMain.Size = new System.Drawing.Size(720, 320);
			this.dgMain.TabIndex = 2;
			// 
			// frmMain
			// 
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(736, 389);
			this.Controls.Add(this.dgMain);
			this.Controls.Add(this.btnStart);
			this.Controls.Add(this.tbReport);
			this.Name = "frmMain";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Form1";
			this.Load += new System.EventHandler(this.frmMain_Load);
			((System.ComponentModel.ISupportInitialize)(this.dgMain)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		#endregion

		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main() 
		{
			Application.Run(new frmMain());
		}

		private void btnStart_Click(object sender, System.EventArgs e)
		{

			/*
			 * Нужно реализовать такие же параметры для работы как и раньше
			 * /c:<number> - запустить отчеты только для данного клиента
			 * /gr:<number> - запустить 
			 
			 */
			//ConfigurationManager.ConnectionStrings["DB"].ConnectionString
			MySqlConnection mc = new MySqlConnection(String.Format("server={0};user id={1}; password={2}; database={3}; pooling=false", "testdb", "system", "123", "temp"));
			mc.Open();
			//MySqlDataAdapter daCombineReports = new MySqlDataAdapter(String.Format("select * from usersettings.CombineReports where {0} = ?{0}", GeneralReport.colAllow), mc);
			//RatingReport rt = new RatingReport(1, 1912, "Test", mc);
			//dgMain.DataSource = rt.GetReport();
			//rt.ExportToExcel((DataTable)dgMain.DataSource);


		}

		//Вспомогательная функция отправки письма
		private void Mail(string From, string MessageTo, string Subject, string Body)
		{
			try
			{
				MailMessage message = new MailMessage(From, 
#if (TESTING)
				"s.morozov@analit.net",
#else
					MessageTo, 
#endif
					Subject, Body);
				SmtpClient Client = new SmtpClient(Settings.Default.SMTPHost);
				message.IsBodyHtml = false;
				message.BodyEncoding = System.Text.Encoding.UTF8;
				Client.Send(message);
			}
			catch
			{
			}
		}

		//Сообщение о глобальной ошибке, возникшей в результате работы программы
		private void MailGlobalErr(string ErrDesc)
		{
			Mail(Properties.Settings.Default.ErrorFrom, Properties.Settings.Default.ErrorReportMail, "Ошибка при запуске программы отчетов", 
				String.Format("Параметры запуска : {0}\r\nОшибка : {1}", String.Join("  ", Environment.GetCommandLineArgs()), ErrDesc));
		}

		//Сообщение об ошибке, возникшей в результате построения общего отчета
		private void MailGeneralReportErr(string ErrDesc, string ShortName, ulong GeneralReportCode)
		{
			Mail(Properties.Settings.Default.ErrorFrom, Properties.Settings.Default.ErrorReportMail, "Ошибка при запуске отчетa для " + ShortName,
				String.Format("Код отчета : {0}\r\nОшибка : {1}", GeneralReportCode, ErrDesc));
		}

		private void frmMain_Load(object sender, EventArgs e)
		{

			try
			{
				//Попытка получить код клиента в параметрах
				int CurrentClientCode = CommandLineUtils.GetCode(@"/c:");
				//Попытка получить код общего отчета в параметрах
				int GeneralReportID = CommandLineUtils.GetCode(@"/gr:");

				string sqlSelectReports;

				if ((CurrentClientCode != -1) || (GeneralReportID != -1))
				{
					MySqlConnection mc = new MySqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString);
					mc.Open();
					try
					{

						//Формируем запрос
						sqlSelectReports =
@"SELECT  cr.*, cd.ShortName  
FROM    reports.general_reports cr, 
        usersettings.clientsdata cd  
WHERE   cr.FirmCode         =cd.firmcode  
        AND cd.firmstatus   =1  
        AND cd.billingstatus=1  
        AND cr.Allow        = 1 ";
						if (GeneralReportID != -1)
						{
							sqlSelectReports += " and cr.generalreportcode=" + GeneralReportID;
						}
						else
						{
							if (CurrentClientCode != -1)
							{
								sqlSelectReports += " and cd.firmcode=" + CurrentClientCode;
							}
						}

						//Выбирает отчеты согласно фильтру
						DataTable dtGeneralReports = MethodTemplate.ExecuteMethod<ReportsExecuteArgs, DataTable>(new ReportsExecuteArgs(sqlSelectReports), GetGeneralReports, null, mc, true, null, false, null);
												
						if (dtGeneralReports != null)
						{
							foreach (DataRow drReport in dtGeneralReports.Rows)
								try
								{
									//Создаем каждый отчет отдельно и пытаемся его сформировать
									GeneralReport gr = new GeneralReport(
										(ulong)drReport[GeneralReportColumns.GeneralReportCode],
										Convert.ToInt32(drReport[GeneralReportColumns.FirmCode]),
										drReport[GeneralReportColumns.EMailAddress].ToString(),
										drReport[GeneralReportColumns.EMailSubject].ToString(),
										mc,
										drReport[GeneralReportColumns.ReportFileName].ToString(),
										drReport[GeneralReportColumns.ReportArchName].ToString());
									gr.ProcessReports();
								}
								catch (Exception ex)
								{
									MailGeneralReportErr(
										ex.ToString(),
										(string)drReport[GeneralReportColumns.ShortName],
										(ulong)drReport[GeneralReportColumns.GeneralReportCode]);
								}
						}
					}
					finally
					{
						mc.Close();
					}
				}
			}
			catch (Exception ex)
			{
				MailGlobalErr(ex.ToString());
			}

			Application.Exit();
		}

		//Выбираем отчеты из базы
		private DataTable GetGeneralReports(ReportsExecuteArgs e)
		{
			e.DataAdapter.SelectCommand.CommandText = e.SQL;
			DataTable res = new DataTable();
			e.DataAdapter.Fill(res);
			return res;
		}

	}

	//Аргументы для выбора отчетов из базы
	internal class ReportsExecuteArgs : ExecuteArgs
	{
		internal string SQL;

		public ReportsExecuteArgs(string sql)
		{
			SQL = sql;
		}
	}

}
