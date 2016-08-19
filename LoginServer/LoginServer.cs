using LoginServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static LoginServer.Protocol.Packet;

namespace LoginServer
{
    class LoginServer
    {
        private Socket listenSocket;
        private Socket socketBE;
        
        private Dictionary<string, Socket> frontEndList = null;

        private MConvert mc = new MConvert();
        private const int HEAD_SIZE = 4;

        private int backlog = 10;
        private bool listening;
        private int port;
        


        Task<Socket> acceptTask = null;

        public LoginServer(int port)
        {
            
            this.port = port;
            listening = true;
        }

        public async void Start(IPAddress ip, int port)
        {
            await Task.WhenAll(BindListenerAsync(this.port), ConnectBEAsync(ip, port));
            while (listening)
            {
                AcceptClient();
            }
            Console.ReadLine();
            Console.WriteLine("[Server]End");
        }

        //bind& connect
        #region
        public Task BindListenerAsync(int port)
        {
            return Task.Run(() => {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);

                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEP);
                listenSocket.Listen(backlog);
            });
        }

        public Task ConnectBEAsync(IPAddress ip, int port)
        {
            return Task.Run(() => {
                try
                {
                    socketBE = new Socket(SocketType.Stream, ProtocolType.Tcp);

                    IPEndPoint remoteEP = new IPEndPoint(ip, port);

                    socketBE.Connect(remoteEP);

                    Console.WriteLine("[Server][Connect] BE");
                }
                catch (Exception)
                {
                    Console.WriteLine("[Server][Connect] BE fail");
                }
                
            });
        }

        #endregion


        //Accept
        #region
        private async void AcceptClient()
        {
            try
            {
                if (acceptTask != null)
                {
                    if (acceptTask.IsCompleted)
                    {
                        Socket client = await AcceptAsync(listenSocket);
                        Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", client.RemoteEndPoint.ToString());
                        Receive(client);
                    }
                }
                else
                {
                    Socket client = await AcceptAsync(listenSocket);
                    Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", client.RemoteEndPoint.ToString());
                    Receive(client);
                }
                
            }
            catch (SocketException)
            {
                Console.WriteLine("[Server][Accept]  Fail.");
            }
            catch (Exception)
            {
                Console.WriteLine("[Server][Accept]  Fail.");
            }

        }

        private Task<Socket> AcceptAsync(Socket socket)
        {
            acceptTask = Task.Run<Socket>(() => socket.Accept());
            return acceptTask;
        }
        #endregion

        //Receive
        #region
        private async void Receive(Socket socket)
        {
            try
            {
                while (socket != null && socket.Connected)
                {
                    Packet? packet = await Task.Run<Packet?>(() => ReceiveAsync(socket));

                    if (packet == null)
                    {
                        throw new SocketException();
                    }
                    else
                    {
                        ProcessRequest((Packet)packet, socket);
                    }
                }


            }
            catch (SocketException)
            {
                Console.WriteLine("[Server][Receive] Client({0}) error", socket.AddressFamily);
            }
            catch (Exception)
            {
                Console.WriteLine("[Server][Receive] Client({0}) error", socket.AddressFamily);

            }

        }

        public Packet? ReceiveAsync(Socket socket)
        {
            Packet packet = new Packet();

            if (socket != null)
            {
                try
                {
                    byte[] buffer = new byte[4];
                    int readBytes = socket.Receive(buffer);

                    if (0 == readBytes)
                    {
                        CloseSocket(socket);
                    }

                    Header header = (Header)mc.ByteToStructure(buffer, typeof(Header));
                    packet.data = null;

                    int bodyLen = header.size;

                    if (bodyLen > 0)
                    {
                        buffer = new byte[bodyLen];
                        readBytes = socket.Receive(buffer);
                        if (0 == readBytes)
                        {
                            CloseSocket(socket);
                        }
                        packet.data = buffer;
                    }
                    Console.WriteLine("[Server][Receive] FrontEnd({0})", socket.RemoteEndPoint.ToString());
                    return packet;
                }
                catch (SocketException)
                {
                    return null;
                }
                catch (Exception)
                {
                    return null;
                }
            }
            return null;

        }
        #endregion

        //Send
        #region
        private async void Send(Socket socket, Packet packet)
        {
            await Task.Run(() => SendAsync(socket, packet));
        }

        public void SendAsync(Socket socket, Packet packet)
        {
            try
            {
                int lenght = HEAD_SIZE + packet.header.size;

                byte[] bytes = new byte[lenght];

                Array.Copy(mc.StructureToByte(packet.header), bytes, HEAD_SIZE);
                Array.Copy(packet.data, 0, bytes, HEAD_SIZE, packet.header.size);

                int sendBytes = socket.Send(bytes);

            }
            catch (SocketException)
            {
                Console.WriteLine("[Server][Send] FrontEnd({0}) error", socket.RemoteEndPoint.ToString());
            }
            catch (Exception)
            {
                Console.WriteLine("[Server][Send] FrontEnd({0}) error", socket.RemoteEndPoint.ToString());
            }
        }
        #endregion

        private void ProcessRequest(Packet packet, Socket socket)
        {

        }

        private void CloseSocket(Socket socket)
        {
            // frontEndList.Remove(socket);
            socket.Close();
            Console.WriteLine("[Server][Close] FrontEnd({0}) ", socket.AddressFamily);
        }


    }

}

