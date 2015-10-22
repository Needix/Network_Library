using System;
using System.Net.Sockets;
using Network_Library.Network.Messaging;

namespace Network_Library.Network.Client {
    public abstract class ClientSocket : ASocket {
        protected ClientSocket(TcpClient client) : base(client) { }
        protected ClientSocket(string host, int port) : base(new TcpClient(host, port)) { }

        /// <summary>
        /// Gets called when a new message arrives.
        /// Default Network Protocol (Server -> Client)
        /// * "CLOSE" -> Client requests server to close connection
        /// * "PING" -> Server pings to check if client is still online, expects to send "PONG"
        /// * "PONG" -> Server sent "PONG", Server is still online/not timed out
        /// </summary>
        /// <param name="cmd">The command that was sent</param>
        /// <param name="param">Optional parameters</param>
        protected internal sealed override void SearchInternalCommand(String cmd, params object[] param) {
            try {
                switch(cmd) {
                    case "CLOSE":
                        Closed = true;
                        RemoteConnection.Close();
                        return;
                    case "PING":
                        Send(new Message("PONG"));
                        SendTimeoutPing = false;
                        return;
                    case "PONG":
                        return;
                }
            } catch(IndexOutOfRangeException) {
                Debug.WriteLine(this, "ERROR: Not enough parameter were sent for cmd \""+cmd+"\"! (parameter: \""+RebuildString(param)+"\")");
                return;
            }
            SearchCommand(cmd, param);
        }

        public sealed override void Close() {
            Send(new Message("REQUEST_CLOSE"));
        }
    }
}
