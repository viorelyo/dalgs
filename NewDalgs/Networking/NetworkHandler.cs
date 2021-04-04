using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace NewDalgs.Networking
{
    class NetworkHandler
    {
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private readonly string _processHost;
        private readonly int _processPort;

        private ManualResetEvent _listenerReady = new ManualResetEvent(false);
        private CancellationToken _ct;
        private CancellationTokenSource _cts = new CancellationTokenSource();

        // TODO maybe implement abstract Publisher and inherit from him (if there will be more publishers)
        public delegate void Notify(NetworkHandler publisher, ProtoComm.Message e);
        public event Notify OnPublish;

        private bool _isRunning;

        public NetworkHandler(string processHost, int processPort)
        {
            _processHost = processHost;
            _processPort = processPort;
        }

        /// <summary>
        /// Wrapping each message into Message(NetworMessage) before sending it through TCP Socket
        /// </summary>
        public void SendMessage(byte[] serializedMsg, string remoteHost, int remotePort)
        {
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
                throw new NetworkException($"Exception occurred in SendMessage", ex);
            }
        }

        public void ListenForConnections()
        {
            _isRunning = true;
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

                    try
                    {
                        _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessConnection), _listener);
                    }
                    catch (SocketException)
                    {
                        Logger.Error($"[{_processPort}]: Could not handle connection. Connection ignored");
                        continue;
                    }

                    _listenerReady.WaitOne();
                }
            }
            catch(SocketException ex)
            {
                throw new NetworkException("Exception occurred in listener", ex);
            }
            finally
            {
                _listener?.Stop();
                Logger.Debug($"[{_processPort}]: Listener Stopped");

                _isRunning = false;
            }
        }

        public void StopListener()
        {
            if (!_isRunning)
                return;     // Listener is already stopped

            if (_ct.IsCancellationRequested)
                return;     // Listener was already cancelled

            _cts?.Cancel();
            _listenerReady.Set();

            Logger.Debug($"[{_processPort}]: Stop Requested");
        }


        /// <summary>
        /// Unwrapping each received message from Message(NetworkMessage)
        /// </summary>
        private void ProcessConnection(IAsyncResult ar)
        {
            if (_ct.IsCancellationRequested)
                return;

            var listener = ar.AsyncState as TcpListener;
            if (listener == null)
                return;

            _listenerReady.Set();

            try
            {
                using (var connection = listener.EndAcceptTcpClient(ar))
                {
                    Logger.Trace($"[{_processPort}]: New connection accepted");
                    if (OnPublish == null)
                        return;

                    using (var networkStream = connection.GetStream())
                    {
                        using (var reader = new BinaryReader(networkStream))
                        {
                            // BinaryWriter / BinaryReader supports only LittleEndian.
                            // We should convert the length to BigEndian, because dalgs.exe (Go) reads in BigEndian
                            var msgLenArr = reader.ReadBytes(4);
                            Array.Reverse(msgLenArr);
                            int msgSize = BitConverter.ToInt32(msgLenArr, 0);

                            byte[] serializedMsg = reader.ReadBytes(msgSize);
                            if (msgSize != serializedMsg.Length)
                            {
                                Logger.Error($"[{_processPort}]: Incomplete message received: [{serializedMsg.Length}/{msgSize}]. Message ignored");
                                return;
                            }

                            var receivedMsg = ProtoComm.Message.Parser.ParseFrom(serializedMsg);

                            OnPublish(this, receivedMsg);
                        }
                    }
                }
            }
            catch (Exception)
            {
                Logger.Error($"[{_processPort}]: Could not receive message from incoming connection. Message ignored");
            }
        }
    }
}
