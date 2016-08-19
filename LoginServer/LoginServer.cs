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
                        Socket client = await AcceptAsync();
                        Console.WriteLine("[Server][Accept]  Client({0}) is Connected.", client.RemoteEndPoint.ToString());
                        Receive(client);
                    }
                }
                else
                {
                    Socket client = await AcceptAsync();
                    Console.WriteLine("[Server][Accept]  Client({0}) is Connected.", client.RemoteEndPoint.ToString());
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

        private Task<Socket> AcceptAsync()
        {
            acceptTask = Task.Run<Socket>(() => {
                Socket socket;
                try
                {
                    Console.WriteLine("Accepting...");
                    socket = listenSocket.Accept();
                }
                catch (Exception)
                {
                    socket = null;
                    Console.WriteLine("err");
                }

                return socket;
            });
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
            catch (SocketException e )
            {
                Console.WriteLine("[Server][Receive] Client({0}) error : {1}", socket.RemoteEndPoint.ToString(), e.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server][Receive] Client({0}) error : {1}", socket.RemoteEndPoint.ToString(), e.ToString());

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

                    packet.header = header;
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
                    Console.WriteLine("[Server][Receive] Client({0})", socket.RemoteEndPoint.ToString());
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
                Console.WriteLine("[Server][Send] Client({0}) error", socket.RemoteEndPoint.ToString());
            }
            catch (Exception)
            {
                Console.WriteLine("[Server][Send] Client({0}) error", socket.RemoteEndPoint.ToString());
            }
        }
        #endregion

        private void ProcessRequest(Packet packet, Socket socket)
        {
            switch (packet.header.code)
            {
                case Command.SIGNIN:
                case Command.SIGNIN_FAIL:
                case Command.SIGNIN_SUCCESS:
                    SignIn(packet, socket);
                    break;
                case Command.SIGNUP:
                case Command.SIGNUP_FAIL:
                case Command.SIGNUP_SUCCESS:
                    break;
                default:
                    break;
            }
        }



        private void SignIn(Packet packet, Socket socket)
        {
            ushort command = packet.header.code;

            if (command == Command.SIGNIN)
            {
                Console.WriteLine("[Server][SignIn] Client({0} Request) ", socket.RemoteEndPoint.ToString());
                CFLoginRequest cfLoginRequest = (CFLoginRequest)mc.ByteToStructure(packet.data, typeof(CFLoginRequest));

                char[] id = cfLoginRequest.id;
                char[] pw = cfLoginRequest.password;
                //string cookie = MakeCookie(id, pw);

                FBLoginRequest fbLoginRequest = new FBLoginRequest(id, pw);

                Packet sendPacket = new Packet();

                sendPacket.data = mc.StructureToByte(fbLoginRequest);
                sendPacket.header.code = command;
                sendPacket.header.size = (ushort)sendPacket.data.Length;

                Send(socketBE, sendPacket);

                CFLoginResponse cfLoginResponse = new CFLoginResponse("10.100.58.4", 41469, "1234124");
                sendPacket.data = mc.StructureToByte(cfLoginResponse);
                sendPacket.header.code = Command.SIGNIN_SUCCESS;
                sendPacket.header.size = (ushort)sendPacket.data.Length;

                Send(socket, sendPacket);
                Console.WriteLine("[Server][SignIn] Success({0}) Response", socket.RemoteEndPoint.ToString());


            }
            else if (command == Command.SIGNIN_FAIL)
            {
                Console.WriteLine("[Server][SignIn] BE({0}) Response", socket.RemoteEndPoint.ToString());
            }
            else if (command == Command.SIGNIN_SUCCESS)
            {

            }
            else if (command == Command.DUMMY_SIGNIN)
            {
            }
            else if (command == Command.DUMMY_SIGNIN_FAIL)
            {
            }
            else if (command == Command.DUMMY_SIGNIN_SUCCESS)
            {
            }


        }

        private void CloseSocket(Socket socket)
        {
            socket.Close();
            Console.WriteLine("[Server][Close] Client({0}) ", socket.RemoteEndPoint.ToString());
        }


    }

}

