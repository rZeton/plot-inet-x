using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Plot_iNET_X.Analyser_Logic
{
    // TODO - plot or txt report with error counters.

    public static class LogItems
    {
        //TO DO
        public static void addParsingError(String errorMsg)
        {
            Globals.errorMsg.AppendFormat(errorMsg);
        }

        public static void addStreamInfo(String streamMsg)
        {
            Globals.streamMsg.AppendFormat(streamMsg);          
        }
    }

}
