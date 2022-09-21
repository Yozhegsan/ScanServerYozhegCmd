using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace ScanServerYozhegCmd
{
    public static class log
    {
        private static string LogFile = Environment.GetFolderPath(Environment.SpecialFolder.CommonDocuments) + "\\ScanServerCmdLog.txt";

        //###############################################################################################################################################################

        public static void Add(string text, bool NewLine = false)
        { System.IO.File.AppendAllLines(LogFile, new string[] { (NewLine ? "\r\n" : "") + DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss - ") + text }, Encoding.Default); }

        public static void Open()
        { try { System.Diagnostics.Process.Start("notepad", LogFile); } catch { } }
    }
}
