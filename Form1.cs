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

namespace Plot_iNET_X
{
    public partial class Form1 : Form
    {
        public selectChannel selChanObj;
        public PlotData GraphPlot;
        public static List<double> dataToPlot;
        public static List<double> dataToPlot2;
        public static List<double> dataToPlot3;
        GraphPane myPane;
        private static RollingPointPairList list;
        private static RollingPointPairList list2;
        private static RollingPointPairList list3;
        public Form1()
        {
            InitializeComponent();
            myPane = zedGraphControl1.GraphPane;
            list = new RollingPointPairList(1024000);
            list2 = new RollingPointPairList(1024000);
            list3 = new RollingPointPairList(1024000);

        }

        private void button2_Click(object sender, EventArgs e)
        {
            OpenFile("PCAP");
            OpenFile("limit");
            Globals.channelsSelected = new Dictionary<int, List<string>>();
            foreach (int stream in Globals.limitPCAP.Keys)
            {
                Globals.channelsSelected[stream] = new List<string>();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            //GetPacket();
            Thread plot = new Thread(new ThreadStart(GetNewGraph));
            //plot.SetApartmentState(ApartmentState.STA);
            plot.Start();
        }

        public void GetPacket()
        {
            OfflinePacketDevice selectedDevice = new OfflinePacketDevice(Globals.filePCAP);
            dataToPlot = new List<double>();
            dataToPlot2 = new List<double>();
            dataToPlot3 = new List<double>();
            // Open the capture file
            using (PacketCommunicator communicator =
                selectedDevice.Open(65536,                                  // portion of the packet to capture
                                                                            // 65536 guarantees that the whole packet will be captured on all the link layers
                                    PacketDeviceOpenAttributes.Promiscuous, // promiscuous mode
                                    1000))                                  // read timeout
            {
                // Read and dispatch packets until EOF is reached
                communicator.ReceivePackets(0, DispatcherHandler);
            }

            for (int x = 0; x != dataToPlot.Count;x++ )
            {
                //Console.WriteLine(x);
                list.Add(x, dataToPlot[x]);
                list2.Add(x, dataToPlot3[x]);
                list3.Add(x, dataToPlot3[x]);
            }
            LineItem myCurve = myPane.AddCurve("data1", list, Color.Red, SymbolType.Diamond);
            myPane.AddCurve("data2", list2, Color.Green, SymbolType.None);
            myPane.AddCurve("data3", list3, Color.MediumPurple, SymbolType.None);
            myCurve.Line.IsVisible = false;
            dataToPlot.Clear();
            dataToPlot2.Clear();
            dataToPlot3.Clear();

            //myPane.XAxis.Scale.Min = 0;
            //myPane.XAxis.Scale.Max = 30;
            //myPane.XAxis.Scale.MinorStep = 1;
            //myPane.XAxis.Scale.MajorStep = 5;

            zedGraphControl1.AxisChange();
            zedGraphControl1.Invalidate();
            this.SuspendLayout();
            this.ResumeLayout(false);
            this.Refresh();
        }
        private static void DispatcherHandler(Packet packet)
        {
            // print packet timestamp and packet length
            //MessageBox.Show(packet.Timestamp.ToString("yyyy-MM-dd hh:mm:ss.fff") + " length:" + packet.Length);
            dataToPlot.Add(packet.Ethernet.Length);
            dataToPlot2.Add(packet.Length);
            dataToPlot3.Add(packet.IpV4.Length);
            //Console.WriteLine(packet.Timestamp.ToString());


        }

        private void GetNewGraph()
        {
            if (this.checkBox1.Checked)
            {
                try
                {
                    selChanObj = new selectChannel();
                    //start new thread to plot the selected channels
                    foreach (int stream in Globals.channelsSelected.Keys)
                    {
                        if (Globals.channelsSelected[stream].Count != 0)
                        {
                            GraphPlot = new PlotData(stream);
                            GraphPlot.SuspendLayout();
                            GraphPlot.ResumeLayout(false);
                            GraphPlot.ShowDialog();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }
        }
        private static void OpenFile(string type)
        {
            OpenFileDialog openFileDialog1 = new OpenFileDialog();
            SaveFileDialog output = new SaveFileDialog();
            output.CheckFileExists = false;
            // Set filter options and filter index.
            switch (type)
            {
                case ("PCAP"):
                    openFileDialog1.Filter = "Configuration Input (.cap)|*.cap;*.pcap|All Files (*.*)|*.*";
                    break;
                case ("XidML"):
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
                        Globals.limitPCAP = LoadLimitsPCAP(openFileDialog1.FileName.ToString());//"limits.csv");
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
            * 
            * */
            Dictionary<int, Dictionary<string, double[]>> limit = new Dictionary<int, Dictionary<string, double[]>>();
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
                        if (!limit.ContainsKey(Convert.ToInt32(parameters[0])))
                        {
                            limit.Add(Convert.ToInt32(parameters[0]), new Dictionary<string, double[]>(){
                                                                            {parameters[6], data}   //add parameter name with relevant data.
                                                                        });
                        }
                        else
                        {
                            limit[Convert.ToInt32(parameters[0])][parameters[6]] = data;          //add parameter name with relevant data.                        
                        }
                    }
                    lineCnt++;
                }

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
                MessageBox.Show(String.Format("Cannot open {0}, please make sure that file is not in use by other program\n{1}", limitFile, e.Message));
            }
            return limit;
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
}
