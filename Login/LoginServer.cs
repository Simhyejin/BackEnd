using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using StackExchange.Redis;
using static Login.MConvert;
using Login.Protocol;
using static Login.Protocol.Packet;
using System.Threading;

namespace Login
{
    class LoginServer
    {
        private Socket listenSocket;
        private Socket listenSocketforFE;
        private Socket listenSocketforAgent;

        private Dictionary<int, Socket> clientList;
        private Dictionary<Socket, int> userList;

        private Dictionary<string, Socket> feSocketList;
        private Dictionary<string, Socket> feNameSocketList;

        private Dictionary<Socket, int> heartBeatList;

        private MConvert mc = new MConvert();
        private const int HEAD_SIZE = 12;

        private int backlog = 10;
        public bool listening;
        public static int userID = 0;

        private MySQL mysql;
        private RedisHandlerForLogin redis;

        bool dbConnected = false;

        Task<bool> inputTask = null;
        Task<Socket> acceptTask = null;
        Task<Socket> acceptFETask = null;
        Task<Socket> acceptAgentTask = null;

        public LoginServer()
        {
            listening = true;

            clientList = new Dictionary<int, Socket>();
            userList = new Dictionary<Socket, int>();
            feSocketList = new Dictionary<string, Socket>();
            feNameSocketList = new Dictionary<string, Socket>();
            heartBeatList = new Dictionary<Socket, int>();

            mysql = new MySQL();
        }

        public async void Start()
        {
            InputUser();
             await Task.WhenAll(BindListenerAsync(), ConnectMySQLAsync(), ConnectRedisAsync());
            dbConnected = true;
           
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
        public Task BindListenerAsync()
        {
            return Task.Run(() =>
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, 10000);
                IPEndPoint localEPForFE = new IPEndPoint(IPAddress.Any, 20000);
                IPEndPoint localEPForAgent = new IPEndPoint(IPAddress.Any, 30000);

                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEP);
                listenSocket.Listen(backlog);

                listenSocketforFE = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocketforFE.Bind(localEPForFE);
                listenSocketforFE.Listen(backlog);

                listenSocketforAgent = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocketforAgent.Bind(localEPForAgent);
                listenSocketforAgent.Listen(backlog);
            });
        }

        public Task ConnectMySQLAsync()
        {
            return Task.Run(() =>
            {
                mysql.Connect();
            });
        }

        public Task ConnectRedisAsync()
        {
            return Task.Run(() =>
            {
                try
                {
                    redis = new RedisHandlerForLogin();

                }
                catch (Exception e )
                {
                    Console.WriteLine("[ Redis ][ Connect ] Fail");
                    Console.WriteLine(e.ToString());
                }

            });
        }
        #endregion


        //Accept
        #region
        public async void AcceptClient()
        {
            if (dbConnected)
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
                            heartBeatList.Add(client, 0);

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

                        heartBeatList.Add(client, 0);

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
                        socket = listenSocket.Accept();
                        return socket;
                    }

                }
                catch (Exception)
                {
                    socket = null;
                }

                return null;
            });
            return acceptTask;
        }

        
        public async void AcceptFE()
        {
            if (dbConnected)
            {
                try
                {
                    if (acceptFETask != null)
                    {
                        if (acceptFETask.IsCompleted)
                        {
                            Task<Socket> recieveTask = null;
                            string feName = null;
                            Socket frontEnd = await AcceptAsyncFE();
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ] FrontEnd({2}) is Connected", "Server", "Accept", frontEnd.RemoteEndPoint.ToString());
                            IPEndPoint remoteEP = (IPEndPoint)frontEnd.RemoteEndPoint;
                            string remote = frontEnd.RemoteEndPoint.ToString();
                            string remoteIP = remoteEP.Address.ToString();
                            int remotePort = remoteEP.Port;

                            feSocketList.Add(remote, frontEnd);

                            feName = redis.AcceptFE(remoteIP, remotePort);

                            feNameSocketList.Add(feName, frontEnd);
                            Receive(frontEnd, recieveTask);
                            heartBeatList.Add(frontEnd, 0);
                        }
                    }
                    else
                    {
                        Task<Socket> recieveTask = null;
                        string feName = null;
                        Socket frontEnd = await AcceptAsyncFE();
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] FrontEnd({2}) is Connected", "Server", "Accept", frontEnd.RemoteEndPoint.ToString());
                        IPEndPoint remoteEP = (IPEndPoint)frontEnd.RemoteEndPoint;
                        string remote = frontEnd.RemoteEndPoint.ToString();
                        string remoteIP = remoteEP.Address.ToString();
                        int remotePort = remoteEP.Port;

                        feSocketList.Add(remote, frontEnd);

                        feName = redis.AcceptFE(remoteIP, remotePort);

                        feNameSocketList.Add(feName, frontEnd);
                        Receive(frontEnd, recieveTask);
                        heartBeatList.Add(frontEnd, 0);
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

        private Task<Socket> AcceptAsyncFE()
        {
            acceptFETask = Task.Run<Socket>(() =>
            {
                Socket socket;
                try
                {
                    socket = listenSocketforFE.Accept();
                }
                catch (Exception e)
                {
                    socket = null;
                    Console.WriteLine(e.ToString());
                }

                return socket;
            });
            return acceptFETask;
        }

        public async void AcceptAgent()
        {
            if (dbConnected)
            {
                try
                {
                    if (acceptAgentTask != null)
                    {

                        Task<Socket> recieveTask = null;
                        if (acceptAgentTask.IsCompleted)
                        {
                            Socket agent = await AcceptAsyncForAgent();
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ] Agent({2}) is Connected", "Server", "Accept", agent.RemoteEndPoint.ToString());

                            Receive(agent, recieveTask);
                        }
                    }
                    else
                    {
                        Task<Socket> recieveTask = null;
                        Socket agent = await AcceptAsyncForAgent();

                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] Agent({2}) is Connected", "Server", "Accept", agent.RemoteEndPoint.ToString());
                        Receive(agent, recieveTask);
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

        private Task<Socket> AcceptAsyncForAgent()
        {

            acceptAgentTask = Task.Run<Socket>(() =>
            {
                Socket socket;
                try
                {
                    //Console.WriteLine("Accepting...");
                    socket = listenSocketforAgent.Accept();
                }
                catch (Exception e)
                {
                    socket = null;
                    Console.WriteLine(e.ToString());
                }

                return socket;
            });
            return acceptAgentTask;
        }
        #endregion
        
        //Receive
        #region
        private async void Receive(Socket socket, Task receiveTask)
        {
            while (socket != null && socket.Connected)
            {
                try
                {
                    if (receiveTask != null)
                    {
                        if (receiveTask.IsCompleted)
                        {
                            Packet? packet = await Task.Run<Packet?>(() => ReceiveAsync(socket));

                            if (packet == null)
                            {
                                ;
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
                            ;
                        }
                        else
                        {
                            ProcessRequest((Packet)packet, socket);
                        }
                    }


                }
                catch (SocketException e)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] ({2}) Socket error : {3}", "ERORR", "Accept", socket.RemoteEndPoint.ToString(), e.ToString());
                    
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] Client({2}) Unhandled Socket error : {3}", "ERORR", "Accept", socket.RemoteEndPoint.ToString(), e.ToString());
                }
            }

        }

        public Packet? ReceiveAsync(Socket socket)
        {
            //Console.WriteLine("Receiving from FE({0})...", socket.RemoteEndPoint.ToString());
            Packet packet = new Packet();

            if (socket != null)
            {
                try
                {
                    if (heartBeatList.Keys.Contains(socket))
                        socket.ReceiveTimeout = 33 * 100;

                    byte[] buffer = new byte[HEAD_SIZE];

                    int readBytes = socket.Receive(buffer);

                    if (heartBeatList.Keys.Contains(socket))
                        heartBeatList[socket] = 0;

                    if (0 == readBytes)
                    {
                        CloseSocket(socket);
                        return null;
                    }
                    else
                    {

                        Header header = mc.BytesToHeader(buffer);
                        packet.header = header;
                        packet.data = null;

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
                        if(header.code!=1000 && header.code!=1002)
                            Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] {3}", "Server", "Receive", header.code, socket.RemoteEndPoint.ToString());
                        return packet;
                    }
                }

                catch (SocketException e)
                {
                    if (!socket.Connected)
                    {
                        CloseSocket(socket);
                        return null;
                    }
                     if (e.ErrorCode == 10060)
                    {

                        if (++heartBeatList[socket] > 3)
                        {
                            CloseSocket(socket);
                            return null;
                        }
                    }
                    else
                    {
                        Console.WriteLine("[ {0,-5} ][ {1,-8} ] {2} Socket error : {3}", "ERORR", "Receive", socket.RemoteEndPoint.ToString(), e.ToString());
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ] {2}) Unhandled error : {3}", "ERORR", "Receive", socket.RemoteEndPoint.ToString(), e.ToString());
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
                if(packet.header.code != 1002)
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] {3}", "Server", "Send", packet.header.code, socket.RemoteEndPoint.ToString());
            }
            catch (SocketException e)
            {
                if (!socket.Connected)
                    CloseSocket(socket);
                else
                {
                    Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] {3} socket error: {4}", "ERORR", "Send", packet.header.code, socket.RemoteEndPoint.ToString(), e.ToString());
                }
            }
            catch (Exception e)
            {
                //CloseSocket(socket);
                Console.WriteLine("[ {0,-5} ][ {1,-8} ][ {2,-4} ] {3} unhandled error: {4}", "ERORR", "Send", packet.header.code, socket.RemoteEndPoint.ToString(), e.ToString());
            }
        }
        #endregion

        private void CloseSocket(Socket socket)
        {
            Console.WriteLine("[ {0,-5} ][ {1,-8} ] {2}", "server", "Close", socket.RemoteEndPoint.ToString());
            feSocketList.Remove(socket.RemoteEndPoint.ToString());
            redis.CloseSocket(socket);
            heartBeatList.Remove(socket);
            
            socket.Close();
        }

 
        private void ProcessRequest(Packet packet, Socket socket)
        {
            switch (packet.header.code)
            {
                //SIGNUP = 100;
                case Command.SIGNUP:
                    SignUp(packet, socket);
                    break;

                //DELETE_USER = 110;
                case Command.DELETE_USER:
                    DeleteUser(packet, socket);
                    break;

                //UPDATE_USER = 120;
                case Command.UPDATE_USER:
                    Update_User(packet, socket);
                    break;
                
                //DUMMY_SIGNUP = 130;
                case Command.DUMMY_SIGNUP:
                    DummySignUp(packet, socket);
                    break;
                
                //SIGNIN = 200;
                case Command.SIGNIN:
                    SignIn(packet, socket);
                    break;

                case Command.JOIN:
                    JoinRoom(packet, socket);
                    break;

                //CONNECTION_PASS_SUCCESS; 
                case Command.CONNECTION_PASS_SUCCESS:
                    ConnectPass_Succ(packet, socket);
                    break;

                //HEARTBEAT = 1000;
                case Command.HEARTBEAT:
                    ReceiveHeartBeat(packet, socket);
                    break;

                // ADVERTISE = 1100;
                case Command.ADVERTISE:
                    Advertise(packet, socket);
                    break;

                default:
                    break;

            }

        }

      
        private string MakeCookie(string id, string password)
        {
            DateTime d = new DateTime();
            StringBuilder data = new StringBuilder();
            data.Append(id);
            data.Append(password);
            data.Append(d.Date);

            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(data.ToString()));

            StringBuilder cookie = new StringBuilder();
            foreach (byte b in hash)
            {
                cookie.AppendFormat("{0:x2}", b);
            }
            return cookie.ToString();
        }

        //L->BE
        public void SignUp(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Signup]Request");
            Header header = packet.header;
            FBSignupRequest signupRequest = (FBSignupRequest)mc.ByteToStructure(packet.data, typeof(FBSignupRequest));

            string usr = new string(signupRequest.user).Split('\0')[0];
            string pw = new string(signupRequest.password).Split('\0')[0];
            bool signup = mysql.CheckDupID(usr);

            Packet sendPacket = new Packet();

            if (signup)
            {
                sendPacket.header.code = Command.SIGNUP_SUCCESS;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                mysql.InsertUser(usr, pw, false);
                Send(socket, sendPacket);
            }
            else
            {
                sendPacket.header.code = Command.SIGNUP_FAIL;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);
            }
            
        }

        //DELETE_USER = 110;
        public void DeleteUser(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Delete]FE({0})", socket.RemoteEndPoint.ToString());
            Header header = packet.header;
            
            string usr = mysql.GetUserNamebyID(header.uid);
            bool deleteUser = mysql.DeleteUser(usr);
            Packet sendPacket = new Packet();


            if (deleteUser)
            {
                sendPacket.header.code = Command.DELETE_USER_SUCCESS;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);

                redis.DeleteUser(socket.RemoteEndPoint.ToString(), packet.header.uid, usr);
            }
            else
            {
                sendPacket.header.code = Command.DELETE_USER_FAIL;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);
            }
        }

        //UPDATE_USER = 120;
        public void Update_User(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Update] FE({0})", socket.RemoteEndPoint.ToString());

            Header header = packet.header;
            FBUpdateUserRequest updateUserReq = (FBUpdateUserRequest)mc.ByteToStructure(packet.data, typeof(FBUpdateUserRequest));

            string username = mysql.GetUserNamebyID(header.uid);

            bool updateUser = mysql.UpdatePassword(username, new string(updateUserReq.password).Split('\0')[0]);
            Packet sendPacket = new Packet();


            if (updateUser)
            {
                sendPacket.header.code = Command.UPDATE_USER_SUCCESS;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);
            }
            else
            {
                sendPacket.header.code = Command.UPDATE_USER_USER_FAIL;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);
            }
        }

        //case Command.DUMMY_SIGNUP:
        public void DummySignUp(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Signup] Dummy Request");
            Header header = packet.header;
            FBDummySignupRequest signupRequest = (FBDummySignupRequest)mc.ByteToStructure(packet.data, typeof(FBDummySignupRequest));

            string usr = new string(signupRequest.user).Split('\0')[0];
            string pw = new string(signupRequest.password).Split('\0')[0];
            bool signup = mysql.CheckDupID(usr);

            Packet sendPacket = new Packet();

            if (signup)
            {
                sendPacket.header.code = Command.DUMMY_SIGNUP_SUCCESS;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                mysql.InsertUser(usr, pw, true);
                Send(socket, sendPacket);
            }
            else
            {
                sendPacket.header.code = Command.DUMMY_SIGNUP_FAIL;
                sendPacket.header.size = 0;
                sendPacket.header.uid = header.uid;
                sendPacket.data = null;

                Send(socket, sendPacket);
            }
        }
        //SIGNIN = 200;

        public void SignIn(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Signin]Request");
            Header header = packet.header;
            FBSigninRequest loginRequest = (FBSigninRequest)mc.ByteToStructure(packet.data, typeof(FBSigninRequest));
            int id = mysql.GetUserID(new string(loginRequest.user).Split('\0')[0]);

            Packet sendPacket = new Packet();
            bool signin = mysql.Login(new string(loginRequest.user).Split('\0')[0], new string(loginRequest.password).Split('\0')[0]);
            bool dupSignIn = redis.DupplicateSignIn(id);
            if (id!=0 && signin && !dupSignIn)
            {
                string cookie = MakeCookie(new string(loginRequest.user), new string(loginRequest.password));

                string ip = null;
                int port = 0;
                string newFE = redis.GetRamdomFE(ref ip, ref port);

                if (newFE != null)
                {
                    FBConnectionPassRequest initReq = new FBConnectionPassRequest(cookie);
                    sendPacket.data = mc.StructureToByte(initReq);
                    sendPacket.header.code = Command.CONNECTION_PASS;
                    sendPacket.header.size = (ushort)sendPacket.data.Length;
                    sendPacket.header.uid = id;

                    Socket fesocket = feSocketList[newFE];
                    Send(fesocket, sendPacket);
                    Console.WriteLine("[Server][Send] Client({0}) {1}", fesocket.RemoteEndPoint.ToString(), sendPacket.header.code);

                    FBSigninResponse LoginResponse = new FBSigninResponse(ip, port, cookie);
                    sendPacket.data = mc.StructureToByte(LoginResponse);
                    sendPacket.header.code = Command.SIGNIN_SUCCESS;
                    sendPacket.header.size = (ushort)sendPacket.data.Length;
                    sendPacket.header.uid = header.uid;
                    Send(socket, sendPacket);
                    Console.WriteLine("[Server][Send] Client({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);

                    return ;
                }
            }
           
            sendPacket.header.code = Command.SIGNIN_FAIL;
            sendPacket.header.uid = header.uid;
            sendPacket.header.size = 0;
            sendPacket.data = null;

            Send(socket, sendPacket);

            if(dupSignIn)
                Console.WriteLine("[Server][Send] Client({0}) Dupplicated Login {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
            else if(id == 0)
                Console.WriteLine("[Server][Send] Client({0}) Not exist User{1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
            else
                Console.WriteLine("[Server][Send] Client({0}) Password Wrong{1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
        }

        //ConnectPass_Succ; 
        public void ConnectPass_Succ(Packet packet, Socket socket)
        {
            Header header = packet.header;
            string username = mysql.GetUserNamebyID(header.uid);
            bool isDummy = mysql.GetUserTypebyID(header.uid);

            redis.ConnectPassSuccess(socket.RemoteEndPoint.ToString(), username, header.uid, isDummy);
        }

        //JOIN = 600;
        public void JoinRoom(Packet packet, Socket socket)
        {
            Header header = packet.header;
            FBRoomJoinRequest joinReq = (FBRoomJoinRequest)mc.ByteToStructure(packet.data, typeof(FBRoomJoinRequest));
            Packet sendPacket = new Packet();

            string username = mysql.GetUserNamebyID(header.uid);

            FrontEnd fe = new FrontEnd();
            int result = redis.GetFERoomInfo(username, joinReq.roomNum, out fe);

            if(result == 1)
            {
                sendPacket.data = null;
                sendPacket.header.uid = header.uid;
                sendPacket.header.code = Command.JOIN_FULL_FAIL;
                sendPacket.header.size = 0;
            }
            else if (result == -1)
            {
                sendPacket.data = null;
                sendPacket.header.uid = header.uid;
                sendPacket.header.code = Command.JOIN_NULL_FAIL;
                sendPacket.header.size = 0;
            }
            else
            {
                string password = mysql.GetPasswordID(header.uid);
                string cookie = MakeCookie(username, password);
                FBConnectionPassRequest connPassReq = new FBConnectionPassRequest(cookie);
                sendPacket.data = mc.StructureToByte(connPassReq);
                sendPacket.header.code = Command.CONNECTION_PASS;
                sendPacket.header.uid = header.uid;
                sendPacket.header.size = (ushort)sendPacket.data.Length;

                Send(feNameSocketList[fe.Name], sendPacket);
                Console.WriteLine("[Server][Redirect] send to FE({0}) ", feNameSocketList[fe.Name]);

                FBRoomJoinRedirectResponse joinRes = new FBRoomJoinRedirectResponse(fe.Ip.ToCharArray(), fe.Port, cookie.ToCharArray());
                sendPacket.data = mc.StructureToByte(joinRes);
                sendPacket.header.code = Command.JOIN_REDIRECT;
                sendPacket.header.uid = header.uid;
                sendPacket.header.size = (ushort)sendPacket.data.Length;

                redis.DeleteUser(feNameSocketList[fe.Name].RemoteEndPoint.ToString(), header.uid, username);
            }

            Send(socket, sendPacket);
            Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);

        }

        //HEARTBEAT = 1000;
        private void ReceiveHeartBeat(Packet packet, Socket socket)
        {
            heartBeatList[socket] = 0;
            Packet sendPacket = new Packet();
            sendPacket.data = null;
            sendPacket.header.uid = 0;
            sendPacket.header.code = Command.HEARTBEAT_SUCCESS;
            sendPacket.header.size = 0;
            Send(socket, sendPacket);
        }

        // ADVERTISE = 1100;
        private void Advertise(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Advertise] FE({0})", socket.RemoteEndPoint.ToString());

            FBAdvertiseRequest advertiseReq = (FBAdvertiseRequest)mc.ByteToStructure(packet.data, typeof(FBAdvertiseRequest));

            string remoteIP = new string(advertiseReq.ip).Split('\0')[0];
            int remotePort = advertiseReq.port;
            string remote = socket.RemoteEndPoint.ToString();
            redis.AddFEinfoForClient(remote, remoteIP, remotePort);
        }

        public static int GenerateUserID()
        {
            Interlocked.Increment(ref userID);
            return userID;
        }
    }


}
