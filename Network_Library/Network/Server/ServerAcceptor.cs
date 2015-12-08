using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Network_Library.Network.Messaging;

namespace Network_Library.Network.Server {
    public delegate void ServerSocketCreatedEventHandler(object sender, ServerSocket socket);

    public class ServerAcceptor {
        private readonly List<ServerSocket> _sClients = new List<ServerSocket>();
        private Thread _listeningThread;
        private int _port;

        private volatile Boolean _close;
        public Boolean Close { get { return this._close; } set { this._close = value; } }

        public event ServerSocketCreatedEventHandler ServerSocketCreated;
        public virtual void OnServerSocketCreated(ServerSocket socket) {
            if(ServerSocketCreated == null) return;
            ServerSocketCreated(this, socket);
        }

        private readonly Type _serverSocketType;

        public ServerAcceptor(Type serverSocketType) {
            if(!serverSocketType.IsSubclassOf(typeof(ServerSocket))) throw new ArgumentException("Type should be subclass of ServerSocket");
            this._serverSocketType = serverSocketType;
        }

        /// <summary>
        /// Broadcasts a message to all clients
        /// </summary>
        /// <param name="message">The message to send</param>
        public void BroadcastMessage(Message message) {
            foreach (ServerSocket socket in _sClients) {
                socket.Send(message);
            }
        }

        /// <summary>
        /// Opens the socket and waits for connections
        /// </summary>
        /// <param name="port">The port the server is listening to</param>
        public void OpenServer(int port) {
            this._port = port;
            this._listeningThread = new Thread(RunServer);
            this._listeningThread.Name = "Server_ConnectionServer";
            this._listeningThread.Start();
        }

        private void RunServer() {
            TcpListener listener = new TcpListener(IPAddress.Any, this._port);
            Debug.WriteLine(this, "Listening on port: " + this._port);
            listener.Start();
            while(!Close) {
                if (listener.Pending()) {
                    TcpClient client = listener.AcceptTcpClient();
                    ServerSocket serverSocket = (ServerSocket)Activator.CreateInstance(this._serverSocketType, new object[] { this, client }); ;
                    this._sClients.Add(serverSocket);
                    ServerSocketCreated(this, serverSocket);
                } else Thread.Sleep(100);
            }

            foreach(ServerSocket sClient in this._sClients) {
                sClient.Close();
            }
        }

        internal void RemoveSocket(ServerSocket socket) {
            this._sClients.Remove(socket);
            if (this._sClients.Count == 0) Close = true;
        }
    }
}
