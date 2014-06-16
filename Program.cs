using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Plot_iNET_X.Analyser_Logic;
using System.Text;
using System.Threading;

namespace Plot_iNET_X
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();            
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainWindow());
        }
    }
    public class Globals
    {
        //checks for Form options
        public static Boolean showErrorSummary = false;
        public static bool usePTP = false;
        public static bool useDumpFiles = false;
        public static bool usePCAP = false;
        public static bool useCompare = false;
        
        //downsampling
        public static bool useDownsample = false;
        public static uint sampleCnt = 15;


        //file counters for output reports --TODO
        public static string errorFile;
        public static int errorFileCnt;
        
        public static string filePCAP { get; set; }
        public static string[] filePCAP_list { get; set; }
        public static string limitfile { get; set; }
        public static string fileCSV { get; set; }

        //statistics
        public static UInt32 fileSize { get; set; }
        public static int totalFrames { get; set; }
        public static Dictionary<int, int[]> framesReceived { get; set; }

        //Log messages
        public static StringBuilder errorMsg { get; set; }
        public static StringBuilder streamMsg { get; set; }    

        //stats error counters                   
        public static Dictionary<int, Dictionary<string, uint>> parError { get; set; }
        public static int totalErrors { get; set; }
        public static Dictionary<int, Dictionary<string, uint>> packetErrors { get; set; }

        //
        public static int streamID = -1;
        public const int inetStart = 0; //0 for udps

        //Parameter data holders
        public static Dictionary<int, uint> streamLength;
        public static Dictionary<string, limitPCAP_Derrived> limitPCAP_derrived {get;set;}//; //Dictionary<String,double[]>> limitPCAP_derrived {get;set;}
        public static Dictionary<int, Dictionary<string, double[]>> limitPCAP { get; set; }
        public static Dictionary<int, List<string>> channelsSelected { get; set; }

        public static double[][][] limitArray { get; set; }

        
        public static string filepcap_TMP { get; set; }
        public static string fileDump = null;
        public static string[] fileDump_list { get; set; }

    }
}
