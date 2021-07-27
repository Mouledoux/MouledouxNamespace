using System;
using System.Net;
using System.Text;
using System.Net.Sockets;

namespace Mouledoux.API
{
    public class EZTCP
    {
        public bool Enabled;

        public string ServerIp = "localhost";
        public int ServerPort = 8080;

        public Action OnNewConnection;
        public Action<string> OnMessageRecieved;
        public Action<Exception> OnMessageRecievedException;

        protected TcpListener ListenServer;
        protected Socket NetSocket;
        protected byte[] ReadBuffer = new byte[1024];
        protected StringBuilder MessageBuffer;


        public EZTCP(string ip, int port)
        {
            ServerIp = ip;
            ServerPort = port;
            MessageBuffer = new StringBuilder();
        }

        public void InitializeNet()
        {
            CloseNet();
            StartServer();
        }

        public void StartServer()
        {
            ListenServer = new TcpListener(IPAddress.Parse(ServerIp), ServerPort);
            ListenServer.Start();
            BeginAcceptConnections();
        }


        public void ProcessMessageBuffer()
        {
            if (!Enabled) return;

            else if (MessageBuffer.Length > 1)
            {
                OnMessageRecieved?.Invoke(MessageBuffer.ToString());
                MessageBuffer.Clear();
            }
        }


        public void CloseNet()
        {
            if (NetSocket != null)
            {
                NetSocket.Close();
                NetSocket = null;
            }
            if (ListenServer != null)
            {
                ListenServer.Stop();
                ListenServer = null;
            }
        }




        #region _Connections
        private void BeginAcceptConnections()
        {
            ListenServer.BeginAcceptSocket(EndAcceptNewSocket, ListenServer);
        }

        private void EndAcceptNewSocket(IAsyncResult async)
        {
            NetSocket = null;
            ReadBuffer = new byte[4096];

            try
            {
                NetSocket = ListenServer.EndAcceptSocket(async);
                OnNewConnection?.Invoke();
            }
            catch (Exception ex)
            {
                string.Format("Exception on new socket: {0}", ex.Message);
            }

            if (NetSocket != null) NetSocket.NoDelay = false;


            BeginReceive();
            BeginAcceptConnections();
        }
        #endregion




        #region _Messaging
        public void StartPush(string msg)
        {
            StartPush(StringToByteArray(msg));
        }

        public bool StartPush(byte[] msg, Action<Exception> exceptionCallback = null)
        {
            if (Enabled)
            {
                try
                {
                    NetSocket.BeginSend(msg, 0, msg.Length, SocketFlags.None, EndPush, null);
                }
                catch (Exception e)
                {
                    exceptionCallback?.Invoke(e);
                }
            }
            return Enabled;
        }

        protected void EndPush(IAsyncResult async)
        {
            NetSocket.EndSend(async);
        }


        public void BeginReceive()
        {
            NetSocket.BeginReceive(ReadBuffer, 0, ReadBuffer.Length, SocketFlags.None, EndReceive, null);
        }

        protected void EndReceive(IAsyncResult async)
        {
            try
            {
                int received = NetSocket.EndReceive(async);
                byte[] tmpArr = new byte[received];
                Buffer.BlockCopy(ReadBuffer, 0, tmpArr, 0, received);

                string msg = ByteArrayToString(tmpArr);

                MessageBuffer.Append(msg);
                BeginReceive();
            }
            catch (Exception e)
            {
                OnMessageRecievedException?.Invoke(e);
            }
        }
        #endregion




        #region _Data Conversion
        public static byte[] StringToByteArray(string str)
        {
            Encoding encoding = Encoding.UTF8;
            return encoding.GetBytes(str);
        }
        public static string ByteArrayToString(byte[] msg)
        {
            Encoding enc = Encoding.UTF8;
            return enc.GetString(msg).TrimEnd((char)0);
        }
        #endregion



        public static string GetLocalIPAddress()
        {
            string localIp = "";
            IPHostEntry host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    localIp = ip.ToString();
                    break;
                }
            }
            return localIp;
        }
    }
}