using LoginServer.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using static LoginServer.MConvert;
using static LoginServer.Protocol.Packet;

namespace LoginServer
{
    class LoginServer
    {
        private Socket listenSocket;
        private Socket socketBE;
        private Dictionary<int, Socket> clientList;
        private Dictionary<Socket, int> userList;
        private Dictionary<Socket, int> heartBeatlist;

        private MConvert mc = new MConvert();
        private const int HEAD_SIZE = 4;

        private int backlog = 10;
        public bool listening;
        private int port;

        bool connected = false;

        Task<Socket> acceptTask = null;
        Task<bool> inputTask = null;

        public static int userID = 0;

        public LoginServer(int port)
        {
            
            this.port = port;
            listening = true;
            clientList = new Dictionary<int, Socket>();
            userList = new Dictionary<Socket, int>();
            heartBeatlist = new Dictionary<Socket, int>();
        }

        public async void Start(IPAddress ip, int port)
        {
            InputUser();
            await Task.WhenAll(BindListenerAsync(this.port), ConnectBEAsync(ip, port));

            connected = true;

            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 20 * 1000;
            timer.Elapsed += new ElapsedEventHandler(SendHeartBeat);
            timer.Start();
        }

        public async void InputUser()
        {
            if (inputTask != null)
            {
                if (inputTask.IsCompleted)
                {
                    listening = await inputAsync();
                }
            }
            else
            {
                listening = await inputAsync();
            }

        }

        private Task<bool> inputAsync()
        {

            inputTask = Task.Run<bool>(() =>
            {
                string input = null;
                KeyType result = mc.TryReadLine(out input);

                if (result == KeyType.Exit)
                {
                    return false;
                }
                else if (result == KeyType.Success)
                {
                    if (input.ToLower() == "quit" || input.ToLower() == "exit" || input.ToLower() == "q")
                        return false;
                    else
                        return true;
                }
                else
                    return true;
            });
            return inputTask;
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
                    if (listening)
                    {
                        socketBE = new Socket(SocketType.Stream, ProtocolType.Tcp);

                        IPEndPoint remoteEP = new IPEndPoint(ip, port);

                        socketBE.Connect(remoteEP);
                        Task<Socket> recieveTask = null;
                        Receive(socketBE, recieveTask);
                        heartBeatlist.Add(socketBE, 0);
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE ","Server", "Connect");
                    }
                    
                }
                catch (Exception)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE fail", "Server", "Connect");
                    ConnectBEAsync(ip, port);
                }
                
            });
        }

        #endregion


        //Accept
        #region
        public async void AcceptClient()
        {
            if (connected)
            {
                try
                {
                    if (acceptTask != null)
                    {
                        Task<Socket> recieveTask = null;
                        if (acceptTask.IsCompleted)
                        {
                            Socket client = await AcceptAsync();
                            int n = GenerateUserID();
                            clientList.Add(n, client);
                            userList.Add(client, n);
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) is Connected", "Server", "Accept", client.RemoteEndPoint.ToString());
                            Receive(client, recieveTask);
                            heartBeatlist.Add(client, 0);
                            
                        }
                    }
                    else
                    {
                        Task<Socket> recieveTask = null;
                        Socket client = await AcceptAsync();
                        int n = GenerateUserID();
                        clientList.Add(n, client);
                        userList.Add(client, n);

                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) is Connected", "Server", "Accept", client.RemoteEndPoint.ToString());
                        Receive(client, recieveTask);

                        heartBeatlist.Add(client, 0);

                    }

                }
                catch (SocketException)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] Fail", "ERORR", "Accept");
                }
                catch (Exception)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] Fail", "ERORR", "Accept");
                }
            }
           

        }

        private Task<Socket> AcceptAsync()
        {
            acceptTask = Task.Run<Socket>(() => {
                Socket socket;
                try
                {
                    if (listening)
                    {
                        //Console.WriteLine("Accepting...");
                        socket = listenSocket.Accept();
                        return socket;
                    }
                    
                }
                catch (Exception)
                {
                    socket = null;
                    //Console.WriteLine("err");
                }

                return null;
            });
            return acceptTask;
        }
        #endregion

        //Receive
        #region
        private async void Receive(Socket socket, Task receiveTask)
        {

            try
            {

                while (socket != null && socket.Connected)
                {

                    if (receiveTask != null)
                    {
                        if (receiveTask.IsCompleted)
                        {
                            Packet? packet = await Task.Run<Packet?>(() => ReceiveAsync(socket));

                            if (packet == null)
                            {
                                break;
                            }
                            else
                            {
                                ProcessRequest((Packet)packet, socket);
                            }
                        }
                    }
                    else
                    {
                        Packet? packet = await Task.Run<Packet?>(() => ReceiveAsync(socket));

                        if (packet == null)
                        {
                            break;
                        }
                        else
                        {
                            ProcessRequest((Packet)packet, socket);
                        }
                    }
                }


            }
            catch (SocketException e )
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) Socket error : {3}", "ERORR", "Accept", socket.RemoteEndPoint.ToString(), e.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) Unhandled Socket error : {3}", "ERORR", "Accept", socket.RemoteEndPoint.ToString(), e.ToString());
            }

        }

        public Packet? ReceiveAsync(Socket socket)
        {
            
            if(socket == socketBE)
                Console.WriteLine("Receiving from BE({0})...", socket.RemoteEndPoint.ToString());
            else
                Console.WriteLine("Receiving from Client({0})...", socket.RemoteEndPoint.ToString());
            
            Packet packet = new Packet();

            if (socket != null && socket.Connected)
            {
                try
                {
                    byte[] buffer = new byte[12];

                    //if(heartBeatlist.Keys.Contains(socket))
                    socket.ReceiveTimeout = 30 * 1000;

                    int readBytes = socket.Receive(buffer);

                    //if (heartBeatlist.Keys.Contains(socket))
                        heartBeatlist[socket] = 0;

                    if (0 == readBytes)
                    {
                        CloseSocket(socket);
                        return null;
                    }

                    
                    Header header = mc.BytesToHeader(buffer);
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
                            return null;
                        }
                        packet.data = buffer;
                    }

                    if (socket == socketBE)
                         if (header.code != 1000 && header.code != 1002)
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] BE({3})", "Server", "Receive", header.code, socket.RemoteEndPoint.ToString());
                    else
                         if (header.code != 1000 && header.code != 1002)
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] Client({3})", "Server", "Receive", header.code, socket.RemoteEndPoint.ToString());

                    return packet;
                }
                catch (SocketException e)
                {
                    if (!socket.Connected)
                        return null;
                    if (socket != socketBE && e.ErrorCode == 10060)
                    {

                        if (++heartBeatlist[socket] > 3)
                        {
                            CloseSocket(socket);
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) Socket error : {3}", "ERORR", "Receive", socket.RemoteEndPoint.ToString(), e.ToString());
                        return null;
                    }
                        
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) Unhandled error : {3}", "ERORR", "Receive", socket.RemoteEndPoint.ToString(), e.ToString());
                    return null;
                }
            }
            return null;

        }
        #endregion

        //Send
        #region
        private void Send(Socket socket, Packet packet)
        {
            
            try
            {   
                byte[] bytes = mc.PacketToBytes(packet);
                int sendBytes = socket.Send(bytes);
                if(packet.header.code!=1000 && packet.header.code != 1002)
                    if (socket == socketBE)
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] BE({3})", "Server", "Send", packet.header.code, socket.RemoteEndPoint.ToString());
                    else
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] Client({3})", "Server", "Send", packet.header.code, socket.RemoteEndPoint.ToString());
            }
            catch (SocketException e)
            {
               if (!socket.Connected)
                    CloseSocket(socket);
                else
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] Clinet({3}) socket error: {4}", "ERORR", "Send", packet.header.code, socket.RemoteEndPoint.ToString(), e.ToString());
                }
                
            }
            catch (Exception e)
            {
                CloseSocket(socket);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] Clinet({3}) unhandled error: {4}", "ERORR", "Send", packet.header.code, socket.RemoteEndPoint.ToString(), e.ToString());
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
                    SignUp(packet, socket);
                    break;

                case Command.HEARTBEAT_SUCCESS:
                    ReceiveHeartBeat(packet, socket);
                    break;

                default:
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] from ({3})", "ERORR", "Receive", packet.header.code, socket.RemoteEndPoint.ToString());
                    break;
            }
        }



        private void SignIn(Packet packet, Socket socket)
        {
            ushort command = packet.header.code;

            if (command == Command.SIGNIN)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client ({2}) Request", "Server", "SignIn", socket.RemoteEndPoint.ToString());

                CFSigninRequest cfLoginRequest = (CFSigninRequest)mc.ByteToStructure(packet.data, typeof(CFSigninRequest));

                char[] id = cfLoginRequest.user;
                char[] pw = cfLoginRequest.password;

                FBSigninRequest fbLoginRequest = new FBSigninRequest(id, pw);

                Packet sendPacket = new Packet();

                sendPacket.data = mc.StructureToByte(fbLoginRequest);
                sendPacket.header.code = command;
                sendPacket.header.size = (ushort)sendPacket.data.Length;
                sendPacket.header.uid = userList[socket];

                Send(socketBE, sendPacket);

                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Requeset to BE({2})", "Server", "SignIn", socketBE.RemoteEndPoint.ToString());


            }
            else if (command == Command.SIGNIN_FAIL)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE({2}) Response ", "Server", "SignIn", socketBE.RemoteEndPoint.ToString());
                Send(clientList[(int)packet.header.uid], packet);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Request to Cleint({2}) ", "Server", "SignIn", clientList[(int)packet.header.uid].RemoteEndPoint.ToString());
            }
            else if (command == Command.SIGNIN_SUCCESS)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE({2}) Response ", "Server", "SignIn", socketBE.RemoteEndPoint.ToString());

                Send(clientList[(int)packet.header.uid], packet);
                
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Response to Cleint({2}) ", "Server", "SignIn", clientList[(int)packet.header.uid].RemoteEndPoint.ToString());
            }
           
        }
        public void SignUp(Packet packet, Socket socket)
        {
            ushort command = packet.header.code;
            if (command == Command.SIGNUP)
            {
                packet.header.uid = userList[socket];
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client ({2}) Request", "Server", "SignUp", socket.RemoteEndPoint.ToString());
                Send(socketBE, packet);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Requeset to BE({2})", "Server", "SignUp", socketBE.RemoteEndPoint.ToString());
            }
            else if (command == Command.SIGNUP_FAIL)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE({2}) Response ", "Server", "SignUp", socketBE.RemoteEndPoint.ToString());
                Send(clientList[(int)packet.header.uid], packet);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Request to Cleint({2}) ", "Server", "SignUp", clientList[(int)packet.header.uid].RemoteEndPoint.ToString());
            }
            else if (command == Command.SIGNUP_SUCCESS)
            {
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE({2}) Response ", "Server", "SignUp", socketBE.RemoteEndPoint.ToString());
                Send(clientList[(int)packet.header.uid], packet);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Request to Cleint({2}) ", "Server", "SignUp", clientList[(int)packet.header.uid].RemoteEndPoint.ToString());
            }
        }
        

        private void CloseSocket(Socket socket)
        {
            if (socket == socketBE)
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] BE({2})", "Server", "Close", socketBE.RemoteEndPoint.ToString());
            else
            {
                clientList.Remove(userList[socket]);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2})", "Server", "Close", socketBE.RemoteEndPoint.ToString());
                userList.Remove(socket);
            }
                

            heartBeatlist.Remove(socket);

            socket.Close();
        }

        private void SendHeartBeat(object sender, ElapsedEventArgs e)
        {

            Packet packet = new Packet();
            packet.header.code = Command.HEARTBEAT;
            packet.header.uid = 0;
            packet.header.size = 0;
            packet.data = null;

            Send(socketBE, packet);
            //Console.WriteLine("[Server][Send]Heartbeat to BE({0})", socketBE.RemoteEndPoint.ToString());
            foreach (Socket socket in clientList.Values)
            {
                packet = new Packet();
                packet.header.code = Command.HEARTBEAT;
                packet.header.uid = userList[socket];
                packet.header.size = 0;
                packet.data = null;

                Send(socket, packet);
                //Console.WriteLine("[Server][Send]Heartbeat to FE({0})", socket.RemoteEndPoint.ToString());
            }
            
        }

        private void ReceiveHeartBeat(Packet packet, Socket socket)
        {
            //Console.WriteLine("[Server][Receive]Heartbeat from FE({0})", socket.RemoteEndPoint.ToString());
            heartBeatlist[socket] = 0;
            Packet sendPacket = new Packet();
            sendPacket.data = null;
            sendPacket.header.uid = 0;
            sendPacket.header.code = Command.HEARTBEAT_SUCCESS;
            sendPacket.header.size = 0;
            Send(socket, sendPacket);
        }

        public static int GenerateUserID()
        {
            Interlocked.Increment(ref userID);
            return userID;
        }
    }


}

