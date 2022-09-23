using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ScanServerYozhegCmd
{
    public class StateObject
    {
        public const int BufferSize = 1024;
        public byte[] buffer = new byte[BufferSize];
        public StringBuilder sb = new StringBuilder();
        public Socket workSocket = null;
    }

    public class AsynchronousSocketListener
    {
        public static string FolderPath = "";
        public static bool ErrorFlag = false;
        public static bool CommandFlag = false;

        //########################################################################################################################

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        public AsynchronousSocketListener() { }

        public static void StartListening()
        {
            IPAddress ipAddress = IPAddress.Any;
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, SrvPort);
            Socket listener = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(100);
                while (true)
                {
                    allDone.Reset();
                    listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                    allDone.WaitOne();
                }
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); log.Add(e.ToString()); }
        }

        public static void AcceptCallback(IAsyncResult ar)
        {
            allDone.Set();
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            StateObject state = new StateObject();
            state.workSocket = handler;
            handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
        }

        public static void ReadCallback(IAsyncResult ar)
        {
            StateObject state = (StateObject)ar.AsyncState;
            using (Socket handler = state.workSocket)
            {
                int bytesRead = 0;
                try { bytesRead = handler.EndReceive(ar); } catch { }
                log.Add("Receive command #" + state.buffer[0]);
                Console.WriteLine("Receive command #" + state.buffer[0] + "\r\n");
                switch (state.buffer[0]) { case 48: Send(handler); break; }
            }
        }

        private static void Send(Socket handler)
        {
            using (var stream = new MemoryStream())
            {
                Bitmap bmp = new Bitmap(Image.FromStream(scan.MemScan()));
                bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                handler.BeginSend(stream.ToArray(), 0, stream.ToArray().Length, 0, new AsyncCallback(SendCallback), handler);
            }
        }

        private static void SendCallback(IAsyncResult ar)
        {
            try
            {
                Socket handler = (Socket)ar.AsyncState;
                int bytesSent = handler.EndSend(ar);
                handler.Shutdown(SocketShutdown.Both);
                handler.Close();
            }
            catch { }
        }

        static int SrvPort = 0;
        static Scanner scan;
        static string ScanFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\Scan";

        public static int Main(String[] args)
        {
            if (args.Count() == 0) { SrvPort = 11111; } else { try { SrvPort = Math.Abs(int.Parse(args[0])); } catch { SrvPort = 11111; } }
            Console.Title = "Scanner Server by Yozheg";
            bool LoadFlag = true;
            Console.WriteLine("Port: " + SrvPort + "\r\n");
            if (!Directory.Exists(ScanFolder)) Directory.CreateDirectory(ScanFolder);
            scan = new Scanner();
            try { scan.Configuration(); } catch { }
            if (LoadFlag) StartListening();
            return 0;
        }
    }
}
