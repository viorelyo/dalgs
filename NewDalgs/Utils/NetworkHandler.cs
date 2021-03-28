using Google.Protobuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NewDalgs.Utils
{
    class NetworkHandler
    {
        private readonly string _processHost;
        private readonly int _processPort;

        private TcpListener _listener;
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
        public void SendMessage(Communication.Message message, string remoteHost, int remotePort)
        {
            // TODO method to be try-excepted

            var networkMsg = new Communication.NetworkMessage
            {
                Message = message,
                SenderHost = _processHost,
                SenderListeningPort = _processPort
            };

            var wrapperMsg = new Communication.Message
            {
                Type = Communication.Message.Types.Type.NetworkMessage,
                NetworkMessage = networkMsg,
                SystemId = message.SystemId,     // TODO read from args
                ToAbstractionId = message.ToAbstractionId,
            };

            byte[] serializedMsg = wrapperMsg.ToByteArray();

            // https://stackoverflow.com/questions/8620885/c-sharp-binary-reader-in-big-endian
            // BinaryWriter / BinaryReader supports only LittleEndian. We should convert the length to BigEndian, because dalgs.exe (Go) reads in BigEndian
            byte[] bigEndianMsgLen = BitConverter.GetBytes(serializedMsg.Length);
            Array.Reverse(bigEndianMsgLen);

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

            Console.WriteLine(String.Format("Message sent to [{0}:{1}]", remoteHost, remotePort));        // TODO replace with logger
        }

        // TODO to be try-excepted
        public void ListenForConnections()
        {
            Console.WriteLine(String.Format("Waiting for requests on port [{0}]", _processPort));        // TODO replace with logger

            var adr = IPAddress.Parse(_processHost);
            _listener = new TcpListener(adr, _processPort);
            _listener.Start();
            _ct = _cts.Token;

            try
            { 
                while(!_ct.IsCancellationRequested)
                {
                    _listenerReady.Reset();

                    _listener.BeginAcceptTcpClient(new AsyncCallback(ProcessConnection), _listener);
                    Console.WriteLine("new begin");

                    _listenerReady.WaitOne();
                }
            }
            finally
            {
                _listener?.Stop();
                Console.WriteLine("Literally stopped");
            }
        }

        public void StopListener()
        {   
            if (_ct.IsCancellationRequested)
                return;     // Listener was already cancelled

            _cts?.Cancel();
            _listenerReady.Set();
            Console.WriteLine("Stop Requested...");
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
                Console.WriteLine("New connection accepted");
            }
        }
    }
}
