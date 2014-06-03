using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Plot_iNET_X
{
    public partial class LogWindow : Form
    {
        public static MethodInvoker UpdateGui;
        System.Timers.Timer updateGuiTimer;
        public LogWindow()
        {
            InitializeComponent();            
            this.Resize += LogWindow_Resize;
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private void LogWindow_Resize(object sender, EventArgs e)
        {
            MinimizeMe();
            this.SuspendLayout();
            this.ResumeLayout(false);
        }

        private void updateGUI_tick(object sender, System.Timers.ElapsedEventArgs e)
        {            
            if ((Globals.errorMsg.Length == 0)&&(Globals.streamMsg.Length == 0)) return;
            if (backgroundWorker1.IsBusy) return;
            backgroundWorker1.RunWorkerAsync();
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {               
            UpdateGui = delegate
            {
                this.textBox2.AppendText(String.Format("{0}\t{1}\n",DateTime.Now, Globals.streamMsg));
                this.textBox2.Refresh();
                this.textBox1.AppendText(Globals.errorMsg.ToString());
            };
            this.Invoke(UpdateGui);

            Globals.streamMsg.Clear();
            Globals.errorMsg.Clear();
        }

        private void LogWindow_Load(object sender, EventArgs e)
        {
            updateGuiTimer = new System.Timers.Timer(5000);
            updateGuiTimer.Elapsed += new System.Timers.ElapsedEventHandler(updateGUI_tick);
            updateGuiTimer.Enabled = true;
        }
        public void RestartTimer()
        {
            this.Show();
            this.Visible = true;
            updateGuiTimer.Start();
        }

        public void HideLog()
        {
            updateGuiTimer.Stop();
            this.WindowState = FormWindowState.Minimized;
            //this.Hide();
        }
        
        private void HideLog(object sender, FormClosingEventArgs e)
        {
            updateGuiTimer.Stop();
            this.WindowState = FormWindowState.Minimized;
            e.Cancel = true;
           
        }
        private void MinimizeMe()
        {
            if (FormWindowState.Minimized == this.WindowState)
            {                
                this.notifyIcon1.Visible = true;
                this.notifyIcon1.ShowBalloonTip(500);
                //this.Hide();
            }

            else if (FormWindowState.Normal == this.WindowState)
            {
                this.notifyIcon1.Visible = false;
            }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            notifyIcon1.Visible = false;
        }        
    }
}
