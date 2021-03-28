using Google.Protobuf;
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NewDalgs.Utils
{
    class NetworkHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _processHost;
        private readonly int _processPort;

        private ManualResetEvent _listenerReady = new ManualResetEvent(false);
        private CancellationToken _ct;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        public NetworkHandler(string processHost, int processPort)
        {
            _processHost = processHost;
            _processPort = processPort;
        }

        /// <summary>
        /// Wrapping each message into Message(NetworMessage) before sending it through TCP Socket
        /// </summary>
        public void SendMessage(ProtoComm.Message message, string remoteHost, int remotePort)
        {
            var networkMsg = new ProtoComm.NetworkMessage
            {
                Message = message,
                SenderHost = _processHost,
                SenderListeningPort = _processPort
            };

            var wrapperMsg = new ProtoComm.Message
            {
                Type = ProtoComm.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMsg,
                SystemId = message.SystemId,
                ToAbstractionId = message.ToAbstractionId,
            };

            byte[] serializedMsg = wrapperMsg.ToByteArray();

            // https://stackoverflow.com/questions/8620885/c-sharp-binary-reader-in-big-endian
            // BinaryWriter / BinaryReader supports only LittleEndian. 
            // We should convert the length to BigEndian, because dalgs.exe (Go) reads in BigEndian
            byte[] bigEndianMsgLen = BitConverter.GetBytes(serializedMsg.Length);
            Array.Reverse(bigEndianMsgLen);

            try
            {
                using (var connection = new TcpClient(remoteHost, remotePort))
                {
                    using (var networkStream = connection.GetStream())
                    {
                        using (var writer = new BinaryWriter(networkStream))
                        {
                            writer.Write(bigEndianMsgLen);
                            writer.Write(serializedMsg);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new NetworkException("Exception occurred in SendMessage", ex);
            }

            Logger.Debug($"[{_processPort}]: Message [{message.Type}] sent to [{remoteHost}:{remotePort}]");     // TODO check if should remove destinatar from log
        }

        public void ListenForConnections()
        {
            Logger.Debug($"[{_processPort}]: Waiting for requests");

            var adr = IPAddress.Parse(_processHost);
            TcpListener _listener = null;

            try
            {
                _listener = new TcpListener(adr, _processPort);
                _listener.Start();
                _ct = _cts.Token;

                while(!_ct.IsCancellationRequested)
                {
                    _listenerReady.Reset();

                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessConnection), _listener);

                    _listenerReady.WaitOne();
                }
            }
            catch(Exception ex)
            {
                throw new NetworkException("Exception occurred in listener", ex);
            }
            finally
            {
                _listener?.Stop();
                Logger.Info($"[{_processPort}]: Listener Stopped");
            }
        }

        public void StopListener()
        {   
            if (_ct.IsCancellationRequested)
                return;     // Listener was already cancelled

            _cts?.Cancel();
            _listenerReady.Set();

            Logger.Debug($"[{_processPort}]: Stop Requested");
        }

        private void ProcessConnection(IAsyncResult ar)
        {
            if (_ct.IsCancellationRequested)
                return;

            var listener = ar.AsyncState as TcpListener;
            if (listener == null)
                return;

            _listenerReady.Set();

            using (var connection = listener.EndAcceptTcpClient(ar))
            {
                Logger.Debug($"[{_processPort}]: New connection accepted");
            }
        }
    }
}
