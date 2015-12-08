using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;
using Network_Library.Network.Client;
using Network_Library.Network.Messaging;
using Network_Library.Network.Server;

namespace Network_Library.Network {
    public delegate void UserConnectEventHandler(object sender, TcpClient remote);
    public delegate void UserDisconnectEventHandler(object sender, TcpClient remote);
    public delegate void IncomingMessageEventHandler(object sender, string message);

    public abstract class ASocket {
        private const int TIME_BETWEEN_PINGS = 5000;
        private const int TIME_TILL_TIMEOUT = 15000;
        
        protected TcpClient RemoteConnection { get; private set; }
        private Stream _stream;
        private StreamWriter _writer;
        private StreamReader _reader;

        private Thread _listeningThread;
        protected Boolean Closed { get; set; }

        protected long LastContact { get; set; }
        private Thread _timeoutChecker;
        protected bool SendTimeoutPing;

        public event UserConnectEventHandler UserConnect;
        public virtual void OnUserConnect(TcpClient remote) {
            if(UserConnect == null) return;
            UserConnect(this, remote);
        }
        public event UserDisconnectEventHandler UserDisconnect;
        public virtual void OnUserDisconnect(TcpClient remote) {
            if(UserDisconnect == null) return;
            UserDisconnect(this, remote);
        }
        public event IncomingMessageEventHandler IncomingMessage;
        public virtual void OnIncomingMessage(string message) {
            if(IncomingMessage == null) return;
            IncomingMessage(this, message);
        }

        protected ASocket(TcpClient client) {
            InitSocket(client);
        }

        internal void InitSocket(TcpClient remote) {
            RemoteConnection = remote;
            _stream = remote.GetStream();
            _writer = new StreamWriter(_stream);
            _reader = new StreamReader(_stream);

            if(this is ClientSocket) Debug.WriteLine(this, "Connected to server: " + GetAddress(remote));
            else Debug.WriteLine(this, "Connected to client: "+GetAddress(remote));

            OnUserConnect(remote);

            _listeningThread = new Thread(WaitForMessage);
            _listeningThread.Name = this.GetType().Name+"_ListeningThread";
            _listeningThread.Start();

            _timeoutChecker = new Thread(CheckForTimeout);
            _timeoutChecker.Name = this.GetType().Name + "_TimeoutChecker";
            _timeoutChecker.Start();
        }

        private void CheckForTimeout() {
            LastContact = Environment.TickCount;
            while (!Closed) {
                if (!SendTimeoutPing && Environment.TickCount - LastContact > TIME_BETWEEN_PINGS) {
                    Send(new Message("PING")); 
                    SendTimeoutPing = true;
                }
                if(Environment.TickCount - LastContact > TIME_TILL_TIMEOUT) Close();
                Thread.Sleep(500);
            }
        }

        private void WaitForMessage() {
            while(!Closed) {
                Message message = Receive();
                if(message == null) continue;
                SearchInternalCommand(message.Command, message.Parameters);
                Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Send a message to the remote connection
        /// </summary>
        /// <param name="message"></param>
        public void Send(Message message) {
            message.Send(_stream, _writer);
        }

        /// <summary>
        /// Wait for a new message from the remote connection
        /// </summary>
        /// <returns>The received message, or null if reading failed</returns>
        private Message Receive() {
            LastContact = Environment.TickCount;

            String recLine;

            //Create Type List
            List<Type> types = new List<Type>();
            while (!("END_TYPES".Equals(recLine = _reader.ReadLine()))) {
                if (recLine == null) continue;
                Type t = Type.GetType(recLine);
                if (t == null) {
                    Debug.WriteLine(this, "Warning: Unknown type for: "+recLine+". This will probably lead to unsuccessful deserialization!");
                    continue;
                }
                types.Add(t);
            }
            Type[] typeArray = types.ToArray();

            //XmlAttributeOverride
            XmlAttributeOverrides aor = new XmlAttributeOverrides();
            XmlAttributes listAttribs = new XmlAttributes();
            for(int i = 0; i < typeArray.Length; i++) {
                Type curParameter = typeArray[i];
                listAttribs.XmlElements.Add(new XmlElementAttribute(curParameter));
            }
            aor.Add(typeof(Message), "ListOfParameter", listAttribs);

            //Serializer
            XmlSerializer ser = new XmlSerializer(typeof(Message), aor, typeArray, new XmlRootAttribute(Message.ELEMENT_NAME), Message.DEFAULT_NAMESPACE);

            //Read XML from stream
            String xml = "";
            while(!("END_OBJECT".Equals(recLine = _reader.ReadLine()))) { //TEST: What happens when remote connection closes? exception?
                xml += recLine + "\n";
            }

            //Create object
            TextReader tr = new StringReader(xml);
            object o = ser.Deserialize(tr);
            tr.Close();

            LastContact = Environment.TickCount;
            return o as Message;
        }

        /// <summary>
        /// Gets called when a new message arrives
        /// Implement your Network protocol here
        /// </summary>
        /// <param name="cmd">The command the server/client sent</param>
        /// <param name="param">Optional parameters</param>
        protected abstract void SearchCommand(String cmd, params object[] param);
        /// <summary>
        /// Searches for internal commands when a new message arrives
        /// </summary>
        /// <param name="cmd">The command the server/client sent</param>
        /// <param name="param">Optional parameters</param>
        protected internal abstract void SearchInternalCommand(String cmd, params object[] param);

        /// <summary>
        /// Closes the connection
        /// </summary>
        public abstract void Close();


        //////////////////////////////////////////Util
        public static string RebuildString(object[] split) { return RebuildString(split, 0); }
        public static string RebuildString(object[] split, int begin) {
            String result = "";
            for (int i = 0; i < split.Length-begin; i++) {
                int curIndex = i + begin;
                result += split[curIndex].ToString();
                if (curIndex < split.Length) result += " ";
            }
            return result;
        }

        public static object[] ShortenArray(object[] split, int begin) {
            int newSize = split.Length - begin;
            object[] result = new object[newSize];
            for(int i = 0; i < newSize; i++) {
                result[i] = split[i + begin];
            }
            return result;
        }
        public static object[] LengthenArray(object[] split, string cmd) {
            object[] result = new object[split.Length+1];
            result[0] = cmd;
            for(int i = 0; i < split.Length; i++) {
                result[i + 1] = split[i];
            }
            return result;
        }

        protected string GetRemoteAddress() { return GetAddress(RemoteConnection); }
        protected string GetAddress(TcpClient client) {
            return client.Client.LocalEndPoint.ToString();
        }
    } 
}