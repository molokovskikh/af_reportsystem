using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Data;
using MySql.Data.MySqlClient;

namespace Inforoom.RatingReport
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

			//
			// TODO: Add any constructor code after InitializeComponent call
			//
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
			this.tbReport.TabIndex = 0;
			this.tbReport.Text = "";
			// 
			// btnStart
			// 
			this.btnStart.Location = new System.Drawing.Point(136, 16);
			this.btnStart.Name = "btnStart";
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
			((System.ComponentModel.ISupportInitialize)(this.dgMain)).EndInit();
			this.ResumeLayout(false);

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
			MySqlConnection mc = new MySqlConnection(String.Format("server={0};user id={1}; password={2}; database={3}; pooling=false", "testdb", "system", "123", "temp"));
			mc.Open();
			MySqlDataAdapter daCombineReports = new MySqlDataAdapter(String.Format("select * from usersettings.CombineReports where {0} = ?{0}", CombineReport.colAllow), mc);
			Rating rt = new Rating(1, 1912, "Test", mc);
			dgMain.DataSource = rt.GetReport();
			rt.ExportToExcel((DataTable)dgMain.DataSource);

		}
	}
}
