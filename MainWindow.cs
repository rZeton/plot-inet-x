using PcapDotNet.Core;
using PcapDotNet.Packets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;
using Plot_iNET_X.Analyser_Logic;

namespace Plot_iNET_X
{

   
    public partial class MainWindow : Form
    {
        public selectChannel selChanObj;
        public PlotData GraphPlot;
        performanceMonitor pmon;
        private LogWindow _logWindow;


        public MainWindow()
        {            
            InitializeComponent();
            pmon = new performanceMonitor();
            System.Timers.Timer updateMonitorClock = new System.Timers.Timer(2000);
            updateMonitorClock.Elapsed += new System.Timers.ElapsedEventHandler(updateMonitor_Tick);
            updateMonitorClock.Enabled = true;

            Globals.streamMsg = new StringBuilder(String.Format("=====Started=====\n"));
            Globals.errorMsg = new StringBuilder();

            _logWindow = new LogWindow();
            Thread logWindow = new Thread(() => _logWindow.ShowDialog());
            //Thread logWindow = new Thread(() => new ErrorSummaryWindow().ShowDialog());
            logWindow.Priority = ThreadPriority.BelowNormal;
            logWindow.IsBackground = true;
            logWindow.Start();
        }

        //Form Settings handlers
        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox2.Checked)
            {
                Globals.filePCAP = null;
                Globals.usePCAP = true;
                Globals.useDumpFiles = false;
                checkBox7.Checked = false;
            }
            else
            {
                Globals.usePCAP = false;
            }
        }
        private void checkBox5_CheckedChanged(object sender, EventArgs e)
        {
            Globals.showErrorSummary = !Globals.showErrorSummary;
        }
        private void checkBox3_CheckedChanged(object sender, EventArgs e)
        {
            Globals.usePTP = !Globals.usePTP;
        }
        private void checkBox7_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox7.Checked)
            {
                Globals.filePCAP = null;
                Globals.usePCAP = false;
                Globals.useDumpFiles = true;
                checkBox2.Checked = false;
            }
            else
            {
                Globals.useDumpFiles = false;
            }
        }
        
        //GUI Events
        private void button2_Click(object sender, EventArgs e)
        {
            this.button1.Visible = true;
            if (Globals.usePCAP)
            {
                OpenFile("PCAP_list");
                string folder = System.IO.Path.GetDirectoryName(Globals.filePCAP_list[0]);
                long size = 0;
                foreach (string file in Globals.filePCAP_list)
                {
                    // 3.
                    // Use FileInfo to get length of each file.
                    System.IO.FileInfo info = new System.IO.FileInfo(file);
                    size += info.Length;
                }
                Globals.fileSize = (UInt32)(size / 1000000); //get MBs
                try
                {
                    this.label3.Text = String.Format("{2} pcaps from {0}\n Total Size = {1} MB",
                                                    folder,
                                                    Globals.fileSize,
                                                    Globals.filePCAP_list.Length);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Cannot open {0} ,please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                    Globals.filePCAP_list, ex.ToString()), "PCAP selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else if (Globals.useDumpFiles) LoadDump();
            else
            {
                OpenFile("PCAP");
                try
                {
                    Globals.fileSize = (UInt32)(new System.IO.FileInfo(Globals.filePCAP).Length / 1000000);
                    this.label3.Text = String.Format("PCAP Name = {0}\n Size = {1} MB",
                        System.IO.Path.GetFileName(Globals.filePCAP),
                        Globals.fileSize);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(String.Format("Cannot open {0} ,please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                    Globals.filePCAP, ex.ToString()), "PCAP selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            flowLayoutPanel1.SuspendLayout();
            flowLayoutPanel1.ResumeLayout(false);
            this.SuspendLayout();
            this.ResumeLayout(false);
            
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //GetPacket();
            //selChanObj = new selectChannel();
            //ThreadPool.QueueUserWorkItem(new WaitCallback(GetNewGraph));

            Thread plot = new Thread(new ThreadStart(GetNewGraph));
            //plot.SetApartmentState(ApartmentState.STA);
            plot.Start();
        }
        private void button3_Click(object sender, EventArgs e)
        {
            //OpenFile("PCAP");
            //this.label3.Text = Globals.filePCAP;
            OpenFile("XidML");   

        }
        private void button4_Click(object sender, EventArgs e)
        {
            OpenFile("limit");            
            try
            {
                this.label4.Text = String.Format("Config Name = {0}\n Streams = {1} ",
                    System.IO.Path.GetFileName(Globals.limitfile),
                    Globals.limitPCAP.Count);//"limits.csv");
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Cannot open {0} or crashed during parsing, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                Globals.limitfile, ex.ToString()), "Packet parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Globals.channelsSelected = new Dictionary<int, List<string>>();

            //foreach (int stream in Globals.limitPCAP.Keys)
            for (int i = 0; i != Globals.limitArray.Length; i++)
            {
                int stream = (int)Globals.limitArray[i][0][0];
                Globals.channelsSelected[stream] = new List<string>();
            }
        }
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFile("Dump_folder");
            if (Globals.fileDump != null)
            {
                this.label1.Text = String.Format("Dump Location =\n{0}",
                Globals.fileDump);
            }
            else this.label1.Text = String.Format("Dump Location not selected. Application main folder will be used");
        }
        
        private void LoadDump()
        {
            OpenFile("LoadFromDump");
            string folder = System.IO.Path.GetDirectoryName(Globals.fileDump_list[0]);
            Globals.fileDump = folder;
            long size = 0;
            foreach (string file in Globals.fileDump_list)
            {
                // 3.
                // Use FileInfo to get length of each file.
                System.IO.FileInfo info = new System.IO.FileInfo(file);
                size += info.Length;
            }
            Globals.fileSize = (UInt32)(size / 1000000); //get MBs
            try
            {
                this.label3.Text = String.Format("{2} Binary data files from {0}\n Total Size = {1} MB",
                                                folder,
                                                Globals.fileSize,
                                                Globals.fileDump_list.Length);
            }
            catch (Exception ex)
            {
                MessageBox.Show(String.Format("Cannot open {0}, please make sure that file is not in use by other program\nRead the rest of the crash report below\n\n\n{1}",
                Globals.fileDump_list, ex.ToString()), "Dump file selection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void toolStripStatusLabel5_Click(object sender, EventArgs e)
        {
            Application.Restart();
        }
        private void toolStripStatusLabel8_Click(object sender, EventArgs e)
        {
            if (_logWindow.WindowState == FormWindowState.Minimized)
            {
                Invoke(new Action(() => { _logWindow.WindowState = FormWindowState.Normal; }));
                //_logWindow.WindowState = FormWindowState.Normal;
                Invoke(new Action(() => { _logWindow.Show(); }));
                //_logWindow.Focus();
                _logWindow.Invoke(new MethodInvoker(_logWindow.RestartTimer));
            }
            else
            {
                _logWindow.Invoke(new MethodInvoker(_logWindow.HideLog));
            }
        }

        private void GetNewGraph()
        {
            try
            {
                selChanObj = new selectChannel();

                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);

                // TODO parallel streams ?
                //Parallel.ForEach(Globals.channelsSelected, kvp =>
                //{
                //    if (Globals.channelsSelected[kvp.Key].Count != 0)
                //    {
                //        GraphPlot = new PlotData(kvp.Key);
                //        GraphPlot.SuspendLayout();
                //        GraphPlot.ResumeLayout(false);
                //        GraphPlot.ShowDialog();
                //    }
                //});

                List<int> streamID = new List<int>();
                foreach (int stream in Globals.channelsSelected.Keys)
                {                    
                    if (Globals.channelsSelected[stream].Count != 0)
                    {
                        streamID.Add(stream);
                        //GraphPlot = new PlotData(stream);
                        //GraphPlot.SuspendLayout();
                        //GraphPlot.ResumeLayout(false);
                    }
                }
                //GraphPlot.ShowDialog();
                Thread t1=null;
                if (Globals.useDumpFiles) t1 = new Thread(() => new PlotData(streamID, "dumpFile").ShowDialog());
                else t1= new Thread(() => new PlotData(streamID).ShowDialog());
                t1.Priority = ThreadPriority.Highest;
                t1.IsBackground = true;
                t1.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }


        private static void OpenFile(string type)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            SaveFileDialog output = new SaveFileDialog();
            output.CheckFileExists = false;
            // Set filter options and filter index.
            switch (type)
            {
                case ("Dump_folder"):
                    folderBrowserDialog1.Description = "Select Folder to Dump Temporary data";
                    break;
                case ("PCAP_list"):
                    folderBrowserDialog1.Description = "Select Folder With PCAPs to parse";
                    break;
                case ("LoadFromDump"):                    
                    folderBrowserDialog1.Description = "Select Folder With Data Dump to parse";
                    //openFileDialog1.Filter = "Packet Dump File (.cap)|*.cap;*.pcap|All Files (*.*)|*.*";
                    break;
                case ("PCAP"):
                    openFileDialog1.Title = "Select Packet File";
                    openFileDialog1.Filter = "Packet Dump File (.cap)|*.cap;*.pcap|All Files (*.*)|*.*";
                    break;
                case ("XidML"):
                    openFileDialog1.Title = "Select DAS Studio XidML configuration file";
                    openFileDialog1.Filter = "Configuration Input (.xidml)|*.xidml|All Files (*.*)|*.*";
                    break;
                case ("limit"):
                    openFileDialog1.Filter = "Configuration Output(.csv)|*.csv|All Files (*.*)|*.*";
                    break;
                case ("error"):
                    output.Filter = "Error Log Output(.csv)|*.csv|All Files (*.*)|*.*";
                    output.FilterIndex = 2;
                    output.Title = "Select Error Log location and name";
                    DialogResult resulterror = output.ShowDialog();
                    if (output.FileName != "")
                    {
                        Globals.errorFile = output.FileName.ToString();
                        Globals.errorFileCnt = 0;
                    }
                    else { Globals.errorFile = "error"; }
                    if (resulterror == DialogResult.Cancel) { Globals.errorFile = "error"; }
                    break;
                //case ("output"):
                //    output.Filter = "Data Log Output(.csv)|*.csv|All Files (*.*)|*.*";
                //    output.FilterIndex = 2;
                //    output.Title = "Select Data Log location and name";
                //    DialogResult resultdata = output.ShowDialog();
                //    if (output.FileName != "")
                //    {
                //        Globals.outputFile = output.FileName.ToString();
                //        Globals.outputFileCnt = 0;
                //    }
                //    else { Globals.outputFile = "data"; }
                //    if (resultdata == DialogResult.Cancel){ Globals.outputFile = "dataOut"; }
                //    break;
            }
            if (type == "error") return;
            if (type == "Dump_folder")
            {                
                DialogResult folderRes = folderBrowserDialog1.ShowDialog();
                if (folderRes == DialogResult.OK)
                {


                    IEnumerable<string> files = System.IO.Directory.EnumerateFiles(folderBrowserDialog1.SelectedPath, "*.*", System.IO.SearchOption.AllDirectories)
                                    .Where(s => s.EndsWith(".dat") || s.EndsWith(".tmp"));
                    MessageBox.Show(String.Format("Location will be cleared now:\n{0}\n\nCOPY YOUR {1} DATA FILES NOW !!! \n Press OK to DELETE.",
                        folderBrowserDialog1.SelectedPath,files.ToList<string>().Count),
                        "Temp Folder Erase", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    foreach (string file in files)
                    {
                        new System.IO.FileInfo(file).Delete();
                    }
                    Globals.fileDump = folderBrowserDialog1.SelectedPath;
                    return;
                }
                else return;
            }
            if (type == "LoadFromDump")
            {
                DialogResult folderRes = folderBrowserDialog1.ShowDialog();
                if (folderRes == DialogResult.OK)
                {
                    // string[] files = System.IO.Directory.GetFiles(folderBrowserDialog1.SelectedPath);

                    IEnumerable<string> files = System.IO.Directory.EnumerateFiles(folderBrowserDialog1.SelectedPath, "*.*", System.IO.SearchOption.AllDirectories)
                                .Where(s => s.EndsWith(".dat") || s.EndsWith(".tmp"));

                    Globals.fileDump_list = new string[files.ToList<string>().Count];
                    int e = 0;
                    foreach (string file in files)
                    {
                        Globals.fileDump_list[e] = file;
                        e++;
                    }
                    return;
                }
                else return;
            }
            if (type == "PCAP_list")
            {
                DialogResult folderRes = folderBrowserDialog1.ShowDialog();
                if (folderRes == DialogResult.OK)
                {
                   // string[] files = System.IO.Directory.GetFiles(folderBrowserDialog1.SelectedPath);
                    
                    IEnumerable<string> files = System.IO.Directory.EnumerateFiles(folderBrowserDialog1.SelectedPath, "*.*", System.IO.SearchOption.AllDirectories)
                                .Where(s => s.EndsWith(".pcap") || s.EndsWith(".cap"));

                    Globals.filePCAP_list = new string[files.ToList<string>().Count];
                    int e = 0;
                    foreach(string file in files)
                    {
                        Globals.filePCAP_list[e] = file;
                        e++;
                    }
                    return;
                }
                else return;
            }
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            openFileDialog1.Title = "Select " + type;
            DialogResult result = openFileDialog1.ShowDialog();
            if (result == DialogResult.OK) // Test result.
            {
                switch (type)
                {
                    case ("PCAP"):
                        Globals.filePCAP = openFileDialog1.FileName.ToString();
                        break;
                    case ("limit"):
                        if (Globals.limitPCAP != null)
                        {
                            Globals.limitPCAP.Clear();
                            Globals.limitArray = null;
                        }
                        Globals.limitPCAP = LoadLimitsPCAP(openFileDialog1.FileName.ToString());
                        Configure.LoadLimitsPCAP(openFileDialog1.FileName.ToString());

                        Globals.limitfile = openFileDialog1.FileName.ToString();                        
                        break;
                    case ("XidML"):
                        output.Filter = "Configuration Out (.csv)|*.csv|All Files (*.*)|*.*";
                        output.FilterIndex = 1;
                        output.Title = "Select Output limit file";
                        DialogResult outres = output.ShowDialog();
                        if (output.FileName != "")
                        {
                            Configure.SaveLimits(openFileDialog1.FileName.ToString(), output.FileName.ToString());
                        }
                        if (outres == DialogResult.Cancel) { Configure.SaveLimits(openFileDialog1.FileName.ToString(), "Limits_File.csv"); }
                        break;
                }
            }
            else Globals.limitPCAP = null;
        }

        
        public static Dictionary<int, Dictionary<string, double[]>> LoadLimitsPCAP(string limitFile)
        {
            /*  Structure of Dictionary:
            *   KEY Stream No. ==
            *                   ==> KEY Parameter name ====
            *                                             ==> parameter data in array
            */
            Dictionary<int, Dictionary<string, double[]>> limit = new Dictionary<int, Dictionary<string, double[]>>();
            //Dictionary<int,Dictionary<string, double[]>> limitDerrived = new Dictionary<int, Dictionary<string, double[]>>(); 
            Dictionary<string, limitPCAP_Derrived> limitDerrived = new Dictionary<string, limitPCAP_Derrived>();
            Dictionary<int, uint> streamLength = new Dictionary<int, uint>();
            string line = null;
            int lineCnt = 0;
            try
            {
                System.IO.StreamReader limitStream = new System.IO.StreamReader(limitFile);
                while (limitStream.Peek() >= 0)
                {
                    line = limitStream.ReadLine();
                    if (lineCnt != 0) //ignore name line 
                    //structure of file shall be as follows: Packet #,Stream ID, Stream Rate, Parameter Number,Parameter Offset,Parameter Name,Parameter Type,Range Maximum,Range Minimum,Limit Maximum,Limit Minimum
                    {
                        string[] parameters = line.Split(',');  // readline
                        double[] data = new double[]{
                            Convert.ToDouble(parameters[0]),    //0 - Stream ID	
                            Convert.ToDouble(parameters[1]),    //1 - Packet Number 
                            Convert.ToDouble(parameters[2]),    //2 - stream rate in Hz
                            Convert.ToDouble(parameters[3]),    //3 - Parameter Number
	                        Convert.ToDouble(parameters[4]),    //4 - Parameter Offset
                            Convert.ToDouble(parameters[5]),    //5 - Parameter Occurences in stream
                            Convert.ToDouble(parameters[7]),    //6 - Parameter Type - 0 = Data / 1= BitVector
                            Convert.ToDouble(parameters[8]),    //7 - Range Maximum 0 = Vector
                            Convert.ToDouble(parameters[9]),    //8 - Range Minimum 0 = Vector
                            Convert.ToDouble(parameters[10]),    //9 - Limit Maximum	 -- to be added manually
                            Convert.ToDouble(parameters[11]),   //10	- Limit Minimum  -- to be added manually
                        };

                        int stream = Convert.ToInt32(parameters[0]);
                        string parName = parameters[6];
                        if (!limit.ContainsKey(stream))
                        {
                            limit.Add(stream, new Dictionary<string, double[]>(){
                                                                    {parName, data}   //add parameter name with relevant data.
                                                                        });
                        }
                        else
                        {
                            limit[stream][parName] = data;          //add parameter name with relevant data.                        
                        }

                        //Derrived parameters handling
                        if (parameters.Length > 12)
                        {
                            if (parameters[12] != "")
                            {
                                //string parName = parameters[6];
                                string[] parametersDerrivedName = parameters[12].Split(';');
                                string[] constantsParams = parameters[13].Split(';');
                                var streamID = Convert.ToInt32(parametersDerrivedName[0]);
                                string[] srcParamList = null;

                                limitPCAP_Derrived dataDerrived = new limitPCAP_Derrived();
                                if (parametersDerrivedName.Length > 2)
                                {
                                    srcParamList = new string[parametersDerrivedName.Length - 1];
                                    for (int i = 1; i != parametersDerrivedName.Length; i++)
                                    {
                                        srcParamList[i - 1] = parametersDerrivedName[i];
                                    }
                                    dataDerrived.srcParametersName = srcParamList;
                                }
                                else
                                {
                                    dataDerrived.srcParameterName = parametersDerrivedName[1];
                                }


                                dataDerrived.streamID = streamID;    //0 - source Stream ID	

                                dataDerrived.const1 = Convert.ToDouble(constantsParams[0]);       //2 - constant 
                                dataDerrived.const2 = Convert.ToDouble(constantsParams[1]);       //2 - constant                         
                                if (constantsParams.Length > 2) dataDerrived.const3 = Convert.ToDouble(constantsParams[2]);
                                if (!limitDerrived.ContainsKey(parName))
                                {
                                    limitDerrived.Add((parName), dataDerrived);
                                }
                                else
                                {
                                    MessageBox.Show(String.Format("Parameter {0} is duplicated, plese check your config file", parName), "Parsing error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                    //limitDerrived[Convert.ToInt32(parameters[0])][parameters[6]] = dataDerrived;          //add parameter name with relevant data.                        
                                }
                            }
                        }
                    }
                    lineCnt++;
                }
                Globals.limitPCAP_derrived = limitDerrived;

                foreach (int stream in limit.Keys)
                {
                    streamLength[stream] = 0;
                    foreach (string par in limit[stream].Keys)
                    {
                        if (limit[stream][par][4] > streamLength[stream])
                        {
                            streamLength[stream] = (uint)(limit[stream][par][4] + (uint)limit[stream][par][5] * 2);
                        }
                    }
                }
                Globals.streamLength = streamLength;
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("Cannot open {0}, please make sure that file is not in use by other program\n\nor Possible parsing error - see msg below:\n{1}", limitFile, e.Message));
            }
            return limit;
        }
 
        //handle CPU and Disk Usage
        private void updateMonitor_Tick(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (backgroundWorker1.IsBusy) return;
            backgroundWorker1.RunWorkerAsync();
        }
        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            //var val =  (int)GC.GetTotalMemory(true);
            this.toolStripStatusLabel3.Text = pmon.getIOUsage();
            //this.toolStripProgressBar1.Value =val; //pmon.getAvailableRAM();
            var cpu = (int)pmon.getCurrentCpuUsage();
            Color colorCPU;
            if (cpu > 100)
            {
                cpu = 100;     
                colorCPU = Color.Red;
            }
            else if(cpu>90)
            { colorCPU = Color.OrangeRed;}
            else colorCPU= Color.Green;
            
            this.BeginInvoke((MethodInvoker)delegate
            {
                this.toolStripProgressBar2.Value = cpu;
                this.toolStripProgressBar2.ToolTipText=cpu.ToString();

                //this.toolStripProgressBar2.BackColor = colorCPU;
                this.toolStripProgressBar2.ForeColor = colorCPU;                
            });


        }




   }

    public partial class selectChannel : Form
    {
        public string[] selectedChannel { get; set; }

        public selectChannel()
        {
            try
            {
                TabControl streamParameterTabControl = new TabControl();
                int maximumX = 0;
                int maximumY = 0;
                TabPage[] tabStream = new TabPage[Globals.limitPCAP.Keys.Count];
                int pktCnt = 0;
                foreach (int stream in Globals.limitPCAP.Keys)
                {
                    List<string> parametersList = new List<string>(Globals.limitPCAP[stream].Keys);
                    double[] data = new double[Globals.limitPCAP[stream].Count];
                    CheckBox[] dataSelect = new CheckBox[data.Length];
                    System.Windows.Forms.Label[] dataLabels = new System.Windows.Forms.Label[data.Length];
                    TableLayoutPanel[] dataColumns = new TableLayoutPanel[data.Length / 20 + 1];
                    FlowLayoutPanel flow = new FlowLayoutPanel();
                    flow.FlowDirection = FlowDirection.LeftToRight;
                    tabStream[pktCnt] = new TabPage();
                    for (int i = 0; i != data.Length; i++)
                    {
                        int whichColumn = i / 20;
                        dataLabels[i] = new System.Windows.Forms.Label();
                        dataLabels[i].Name = i.ToString();
                        dataLabels[i].AutoSize = false;
                        //dataLabels[i].Text = String.Format("P{0}", i.ToString().PadLeft(3, '0'));     // Changed to parameter name as ACLS has XX123 convention
                        dataLabels[i].Text = String.Format("{0}", parametersList[i].ToString());
                        dataLabels[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);

                        dataLabels[i].Size = dataLabels[i].PreferredSize;
                        dataSelect[i] = new CheckBox();
                        dataSelect[i].Name = String.Format("{0}", parametersList[i].ToString());
                        dataSelect[i].AutoSize = false;
                        dataSelect[i].Font = new Font(dataLabels[i].Font.FontFamily, 8, dataLabels[i].Font.Style);
                        //Globals.dataHolders[i].TextAlign = ContentAlignment.BottomLeft;
                        dataSelect[i].Size = dataSelect[i].PreferredSize;

                        if (i % 20 == 0)
                        {
                            dataColumns[whichColumn] = new TableLayoutPanel();
                            dataColumns[whichColumn].ColumnCount = 2;
                            dataColumns[whichColumn].RowCount = 20;
                        }
                        dataColumns[whichColumn].Controls.Add(dataLabels[i]);
                        dataColumns[whichColumn].Controls.Add(dataSelect[i]);
                        dataColumns[whichColumn].Size = dataColumns[whichColumn].PreferredSize;
                        if (dataColumns[whichColumn].Size.Height > maximumY) maximumY = dataColumns[whichColumn].Size.Height;
                        //if (tabStream[pktCnt].Size.Width > maximumX) maximumX = tabStream[pktCnt].Size.Width;
                    }
                    for (int i = 0; i != dataColumns.Length; i++)
                    {
                        flow.Controls.Add(dataColumns[i]);
                    }
                    flow.SuspendLayout();
                    flow.ResumeLayout(false);
                    //tabStream[pktCnt].AutoScroll = true;
                    //tabStream[pktCnt].AutoScrollPosition = new System.Drawing.Point(349, 0);
                    flow.Size = flow.PreferredSize;
                    tabStream[pktCnt].Controls.Add(flow);
                    tabStream[pktCnt].Name = stream.ToString();
                    tabStream[pktCnt].Text = String.Format("ID={0}", stream);
                    tabStream[pktCnt].Size = tabStream[pktCnt].PreferredSize;
                    if (tabStream[pktCnt].Size.Height > maximumY) maximumY = tabStream[pktCnt].Size.Height;
                    if (tabStream[pktCnt].Size.Width > maximumX) maximumX = tabStream[pktCnt].Size.Width;
                    pktCnt++;
                }
                foreach (TabPage stream in tabStream)
                {
                    streamParameterTabControl.Controls.Add(stream);
                }
                streamParameterTabControl.SuspendLayout(); streamParameterTabControl.ResumeLayout(false);
                streamParameterTabControl.Size = new Size(maximumX + 5, maximumY + 15);//streamParameterTabControl.PreferredSize;
                //this.Size = this.PreferredSize;
                FlowLayoutPanel selectionFlow = new FlowLayoutPanel();
                selectionFlow.FlowDirection = FlowDirection.LeftToRight;
                selectionFlow.Controls.Add(streamParameterTabControl);
                //streamParameterTabControl.Dock = DockStyle.Fill;                
                Button btnOK = new Button();
                btnOK.Text = "Draw";
                btnOK.Click += new EventHandler(selectChannelClick);
                selectionFlow.Controls.Add(btnOK);
                Button btnAll = new Button();
                btnAll.Text = "All";
                btnAll.Click += new EventHandler(selectAllClick);
                selectionFlow.Controls.Add(btnAll);

                selectionFlow.Size = selectionFlow.PreferredSize;
                selectionFlow.SuspendLayout();
                selectionFlow.ResumeLayout(false);
                this.Controls.Add(selectionFlow);
                this.Size = this.PreferredSize;//new Size(maximumX + 50, maximumY + 45);
                this.SuspendLayout();
                this.ResumeLayout(false);
                this.ShowDialog();
                this.Refresh();
                // for future use // DrawParametersList();
            }
            catch (Exception e)
            {
                MessageBox.Show(String.Format("No Stream ID was selected or StreamID={0} is not correct, check your limits file.\n\n{1}", Globals.streamID, e.Message.ToString()));
            }
        }
        private void selectChannelClick(object sender, EventArgs e)
        {
            getSelectedChannel();
        }
        private void getSelectedChannel()
        {
            Dictionary<int, List<string>> channels = new Dictionary<int, List<string>>(Globals.limitPCAP.Keys.Count);

            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream].Clear();
                channels[stream] = new List<string>();// new String[Globals.limitPCAP[stream].Count];

                int i = 0;
                foreach (Control panel in this.Controls)
                {
                    if (panel is FlowLayoutPanel)
                    {
                        foreach (Control ctr in panel.Controls)
                        {
                            if (ctr is TabControl)
                            {
                                foreach (Control tab in ctr.Controls)
                                {
                                    if ((tab is TabPage) && (tab.Name == stream.ToString()))
                                    {
                                        foreach (Control flow in tab.Controls)
                                        {
                                            if (flow is FlowLayoutPanel)
                                            {
                                                foreach (Control col in flow.Controls)
                                                {
                                                    if (col is TableLayoutPanel)
                                                    {
                                                        foreach (Control tickBox in col.Controls)
                                                        {
                                                            if (tickBox is CheckBox)
                                                            {
                                                                CheckBox tickStat = tickBox as CheckBox;
                                                                if (tickStat.Checked)
                                                                {
                                                                    channels[stream].Add(tickStat.Name);
                                                                    i++;
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Globals.channelsSelected = channels;
            this.Close();
            this.Dispose();

        }

        private void selectAllClick(object sender, EventArgs e)
        {
            getAllChannels();
        }

        private void getAllChannels()
        {
            Dictionary<int, List<string>> channels = new Dictionary<int, List<string>>(Globals.limitPCAP.Keys.Count);

            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream].Clear();
                channels[stream] = new List<string>();// new String[Globals.limitPCAP[stream].Count];

                int i = 0;
                foreach (string parName in Globals.limitPCAP[stream].Keys)
                {
                    channels[stream].Add(parName);
                    i++;
                }
            }
            Globals.channelsSelected = channels;
            this.Close();
            this.Dispose();
        }
    }

    public class performanceMonitor
    {
        System.Diagnostics.PerformanceCounter cpuCounter;
        System.Diagnostics.PerformanceCounter ioCounter;
        
        System.Diagnostics.Process proces; 
        public performanceMonitor()
        {
            proces = System.Diagnostics.Process.GetCurrentProcess();
            string instanceName = System.IO.Path.GetFileNameWithoutExtension(proces.MainModule.FileName);

            cpuCounter = new System.Diagnostics.PerformanceCounter();
            
            cpuCounter.CategoryName = "Process";
            cpuCounter.CounterName = "% Processor Time";
            cpuCounter.InstanceName = instanceName;

            ioCounter = new System.Diagnostics.PerformanceCounter();
            ioCounter.CategoryName = "Process";
            ioCounter.CounterName = "IO Read Bytes/sec";
            ioCounter.InstanceName = instanceName;
            
            //totalRam = new System.Diagnostics.PerformanceCounter("Memory", "Total Physical Memory");
            //totalRam = GC.//new System.Diagnostics.PerformanceCounter("Memory", "Total Mbytes");

        }
        public float getCurrentCpuUsage()
        {
            return cpuCounter.NextValue();
        }

        public string getIOUsage()
        {
            var value = (ioCounter.NextValue() / 1000) + " kB/s";// proces.PrivateMemorySize64 / totalRam.RawValue ;// ramCounter.NextValue();
            return value;// / totalRam.NextValue();

        }

        
    }
}
