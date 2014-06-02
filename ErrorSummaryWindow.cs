using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace Plot_iNET_X
{
public partial class ErrorSummaryWindow : Form
{
    //private Dictionary<int, Dictionary<string,uint>> _errorList;
    private PointPairList[] _errorTotals;

    public ErrorSummaryWindow()
    {
        InitializeComponent();
        zedGraphControl1.ContextMenuBuilder += new ZedGraphControl.ContextMenuBuilderEventHandler(Add_item_toMenu);
        zedGraphControl1.GraphPane.IsFontsScaled = false;
        PrepareErrorList();
        CreateErrorChart(zedGraphControl1);
    }

    #region EditMenu
    private void Add_item_toMenu(ZedGraphControl sender, ContextMenuStrip menuStrip, Point mousePt, ZedGraphControl.ContextMenuObjectState objState)
    {
        foreach (ToolStripMenuItem item in menuStrip.Items)
        {
            if ((string)item.Tag == "save_as")
            {
                menuStrip.Items.Remove(item);
                break;
            }
        }
        ToolStripMenuItem cm = new ToolStripMenuItem();
        cm.Tag = "hide_legend";
        cm.Name = "hide_legend";
        cm.Text = "Hide Legend";
        cm.Click += new EventHandler(HideLegend);
        menuStrip.Items.Add(cm);

        ToolStripMenuItem scroll = new ToolStripMenuItem();
        scroll.Tag = "toggle_scroll";
        scroll.Name = "toggle_scroll";
        scroll.Text = "Toggle Mouse Zoom / Scroll";
        scroll.Click += new EventHandler(ToggleZoom);
        menuStrip.Items.Add(scroll);
    }
    private void ToggleZoom(object sender, EventArgs e)
    {
        //zedGraphControl1.ZoomButtons = MouseButtons.None;
        //zedGraphControl1.ZoomButtons2 = MouseButtons.None;
        //zedGraphControl1.ZoomStepFraction = 0;
        if (zedGraphControl1.PanModifierKeys == Keys.None)
        {
            zedGraphControl1.PanButtons = MouseButtons.Left;
            zedGraphControl1.PanModifierKeys = Keys.Control;

            zedGraphControl1.ZoomButtons = MouseButtons.Left;
            zedGraphControl1.ZoomModifierKeys = Keys.None;

        }
        else
        {
            zedGraphControl1.PanButtons = MouseButtons.Left;
            zedGraphControl1.PanModifierKeys = Keys.None;

            zedGraphControl1.ZoomButtons = MouseButtons.Left;
            zedGraphControl1.ZoomModifierKeys = Keys.Control;

        }


        //= !zedGraphControl1.IsEnableWheelZoom;
        zedGraphControl1.Refresh();
    }
    private void HideLegend(object sender, EventArgs e)
    {

        zedGraphControl1.GraphPane.Legend.IsVisible = !zedGraphControl1.GraphPane.Legend.IsVisible;
        //zedGraphControl1.GraphPane.CurveList.Clear();
        zedGraphControl1.Refresh();
    }
    #endregion EditMenu

    private void PrepareErrorList()
    {
        //_errorList = new PointPairList[Globals.packetErrors.Count]();
        _errorTotals = new PointPairList[Globals.packetErrors.Count];
        int streamCnt=0;
        foreach(int stream in Globals.packetErrors.Keys)
        {
            _errorTotals[streamCnt] = new PointPairList(new double[] { (double)streamCnt },
                  new double[] { Globals.packetErrors[stream]["total"],
                                 Globals.packetErrors[stream]["SEQ"],
                                 Globals.packetErrors[stream]["pktLost"]});
            streamCnt++;
        }
    }
    public void CreateErrorChart(ZedGraphControl zgc)
    {
        GraphPane myPane = zgc.GraphPane;

        // Set the title and axis labels
        myPane.Title.Text = "Errors Found In the PCAP";
        myPane.XAxis.Title.Text = "Stream ID";
        myPane.YAxis.Title.Text = "Error Count";

        
        // create the curves
        BarItem[] bars = new BarItem[_errorTotals.Length];
        for(int i=0;i!=_errorTotals.Length;i++)
        {
            bars[i] = myPane.AddBar(i.ToString(), _errorTotals[i], Color.Bisque);

        }
        //BarItem myCurve = myPane.AddBar("curve 1", list, Color.Blue);
        //BarItem myCurve2 = myPane.AddBar("curve 2", list2, Color.Red);
        //BarItem myCurve3 = myPane.AddBar("curve 3", list3, Color.Green);

        // Fill the axis background with a color gradient
        myPane.Chart.Fill = new Fill(Color.White,
            Color.FromArgb(255, 255, 166), 45.0F);

        zgc.AxisChange();

        // expand the range of the Y axis slightly to accommodate the labels
        myPane.YAxis.Scale.Max += myPane.YAxis.Scale.MajorStep;

        // Create TextObj's to provide labels for each bar
        BarItem.CreateBarLabels(myPane, false, "f0");

    }


    public void CreateErrorChartExample(ZedGraphControl zgc)
    {
        GraphPane myPane = zgc.GraphPane;

        // Set the title and axis labels
        myPane.Title.Text = "Errors Found In the PCAP";
        myPane.XAxis.Title.Text = "Stream ID";
        myPane.YAxis.Title.Text = "Error Count";
 
        PointPairList list = new PointPairList();
        PointPairList list2 = new PointPairList();
        PointPairList list3 = new PointPairList();
        Random rand = new Random();
 
        // Generate random data for three curves
        for ( int i=0; i<5; i++ )
        {
            double x = (double) i;
            double y = rand.NextDouble() * 1000;
            double y2 = rand.NextDouble() * 1000;
            double y3 = rand.NextDouble() * 1000;
            list.Add( x, y );
            list2.Add( x, y2 );
            list3.Add( x, y3 );
        }
 
        // create the curves
        BarItem myCurve = myPane.AddBar( "curve 1", list, Color.Blue );
        BarItem myCurve2 = myPane.AddBar( "curve 2", list2, Color.Red );
        BarItem myCurve3 = myPane.AddBar( "curve 3", list3, Color.Green );
 
        // Fill the axis background with a color gradient
        myPane.Chart.Fill = new Fill( Color.White,
            Color.FromArgb( 255, 255, 166), 45.0F );
 
        zgc.AxisChange();
 
        // expand the range of the Y axis slightly to accommodate the labels
        myPane.YAxis.Scale.Max += myPane.YAxis.Scale.MajorStep;
 
        // Create TextObj's to provide labels for each bar
        BarItem.CreateBarLabels( myPane, false, "f0" );

    }
        
        
    public void CreatePieChart()
    {
        GraphPane pane = zedGraphControl1.GraphPane;
        pane.AddPieSlice(55.444,Color.Red,0.2,"Error");
        pane.AddPieSlice(13.444, Color.Blue, 0.0, "type2");
        pane.AddPieSlice(123.444, Color.PowderBlue, 0.0, "type2");
        pane.AddPieSlice(123.444, Color.Plum, 0.0, "type2");
        pane.AddPieSlice(12222113.444, Color.Salmon, 0.0, "type2");
        Graphics g = CreateGraphics();
        pane.AxisChange(g);
        g.Dispose();
        //Although it is technically possible to combine pie charts with line graphs on the same GraphPane, it is not recommended.
        //If a particular GraphPane contains only PieItem objects, then the AxisChange() 
        //method will automatically make the axes invisible by setting the Axis.IsVisible property to false.
        //The following is an example of a ZedGraph pie chart:       

    }
}
}
