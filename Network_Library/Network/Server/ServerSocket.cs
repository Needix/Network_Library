using System;
using System.Net.Sockets;
using Network_Library.Network.Messaging;

namespace Network_Library.Network.Server {
    public abstract class ServerSocket : ASocket {
        private readonly ServerAcceptor _acceptor;

        protected ServerSocket(ServerAcceptor acceptor, TcpClient tcpClient) : base(tcpClient) {
            this._acceptor = acceptor;
        }
        
        /// <summary>
        /// Gets called when a new message arrives.
        /// Default Network Protocol (Client -> Server)
        /// * "REQUEST_CLOSE" -> Client requests server to close connection
        /// * "PING" -> Client pings to check if server is still online, expects to send "PONG"
        /// * "PONG" -> Client sent "PONG", client is still online/not timed out
        /// </summary>
        /// <param name="cmd">The command that was sent</param>
        /// <param name="param">Optional parameters</param>
        protected internal sealed override void SearchInternalCommand(String cmd, params object[] param) {
            try {
                switch(cmd) {
                    case "REQUEST_CLOSE":
                        Close();
                        return;
                    case "PING":
                        Send(new Message("PONG"));
                        SendTimeoutPing = false;
                        return;
                    case "PONG":
                        return;
                }
            } catch (IndexOutOfRangeException) {
                Debug.WriteLine(this, "ERROR: Not enough parameter were sent for cmd \""+cmd+"\"! (parameter: \""+RebuildString(param)+"\")");
                return;
            }
            SearchCommand(cmd, param);
        }

        public sealed override void Close() {
            Send(new Message("CLOSE"));
            OnUserDisconnect(RemoteConnection);
            _acceptor.RemoveSocket(this);
            Closed = true;
        }
    }
}
