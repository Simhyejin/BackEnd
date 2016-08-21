using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BackEnd.Protocol;
using static BackEnd.Protocol.Packet;
using Project2.Protocol;
using System.Linq;
using System.Threading;

namespace BackEnd
{
    class BackEnd
    {
        private Socket listenSocket;
        private Dictionary<string, Socket> feSocketList;
        private Dictionary<string, Socket> feNameSocketList;
        private Dictionary<Socket, int> heartBeatList;

        private MConvert mc = new MConvert();
        private const int HEAD_SIZE = 12;

        private int backlog = 10;
        public bool listening;
        private int port;

        private MySQL mysql;
        private RedisController redis;
        bool dbConnected = false;


        Task<Socket> acceptTask = null;

        public BackEnd(int port)
        {
            this.port = port;
            listening = true;

            feSocketList = new Dictionary<string, Socket>();
            feNameSocketList = new Dictionary<string, Socket>();
            heartBeatList = new Dictionary<Socket, int>();

            mysql = new MySQL();
            

        }

        public async void Start()
        {
            await Task.WhenAll(BindListenerAsync(port), ConnectMySQLAsync(), ConnectRedisAsync());
            dbConnected = true;
        }

        //bind& connect
        #region
        public Task BindListenerAsync(int port)
        {
            return Task.Run(() =>
            {
                IPEndPoint localEP = new IPEndPoint(IPAddress.Any, port);

                listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                listenSocket.Bind(localEP);
                listenSocket.Listen(backlog);
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
                RedisSet();
                redis.ClearDB();
            });
        }


        private void RedisSet()
        {
            //Connect with Redis DB
            try
            {
                redis = RedisController.RedisInstance;

            }
            catch (Exception)
            {
                Console.WriteLine("[ Redis ][ Connect ] Fail");
            }

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
                            string feName = null;
                            Socket frontEnd = await AcceptAsync();
                            IPEndPoint remoteEP = (IPEndPoint)frontEnd.RemoteEndPoint;
                            string remote = frontEnd.RemoteEndPoint.ToString();
                            string remoteIP = remoteEP.Address.ToString();
                            int remotePort = remoteEP.Port;
                            if (!remoteIP.Equals("10.100.58.4"))
                            {
                                feSocketList.Add(remote, frontEnd);
                                redis.AddFEList(remoteIP, remotePort);

                                feName = redis.AddFEConnectedInfo(remoteIP, remotePort);
                                feNameSocketList.Add(feName, frontEnd);

                            }
                            Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", frontEnd.RemoteEndPoint.ToString());
                            Receive(frontEnd, recieveTask);

                            heartBeatList.Add(frontEnd, 0);
                        }
                    }
                    else
                    {

                        Task<Socket> recieveTask = null;
                        string feName = null;
                        Socket frontEnd = await AcceptAsync();
                        IPEndPoint remoteEP = (IPEndPoint)frontEnd.RemoteEndPoint;
                        string remote = frontEnd.RemoteEndPoint.ToString();
                        string remoteIP = remoteEP.Address.ToString();
                        int remotePort = remoteEP.Port;
                        if (!remoteIP.Equals("10.100.58.4"))
                        {
                            feSocketList.Add(remote, frontEnd);
                            redis.AddFEList(remoteIP, remotePort);

                            feName = redis.AddFEConnectedInfo(remoteIP, remotePort);
                            feNameSocketList.Add(feName, frontEnd);

                        }
                        Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", frontEnd.RemoteEndPoint.ToString());
                        Receive(frontEnd, recieveTask);

                        heartBeatList.Add(frontEnd, 0);
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
            

        }

        private Task<Socket> AcceptAsync()
        {
           
            acceptTask = Task.Run<Socket>(() =>
            {
                Socket socket;
                try
                {
                    Console.WriteLine("Accepting...");
                    socket = listenSocket.Accept();
                }
                catch (Exception e)
                {
                    socket = null;
                    Console.WriteLine(e.ToString());
                }

                return socket;
            });
            return acceptTask;
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
                    Console.WriteLine("[Server][Receive] 1 {0}", e.ToString());
                    Console.WriteLine("[Server][Receive] FrontEnd({0}) socket error", socket.RemoteEndPoint.ToString());
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Server][Receive] 2 {0}", e.ToString());
                    Console.WriteLine("[Server][Receive] FrontEnd({0}) error", socket.RemoteEndPoint.ToString());

                }
            }

        }

        public Packet? ReceiveAsync(Socket socket)
        {
            Console.WriteLine("Receiving from FE({0})...", socket.RemoteEndPoint.ToString());
            Packet packet = new Packet();

            if (socket != null)
            {
                try
                {
                    socket.ReceiveTimeout = 33 * 1000;

                    byte[] buffer = new byte[HEAD_SIZE];
                    int readBytes = socket.Receive(buffer);
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
                        if(header.code!=1000||header.code!=10002)
                            Console.WriteLine("[Server][Receive][{0}] FrontEnd({1})", header.code, socket.RemoteEndPoint.ToString());
                        return packet;
                    }
                }

                catch (SocketException e)
                {
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
                        Console.WriteLine("[Server][Receive]3 {0}", e.ToString());
                        Console.WriteLine("[Server][Receive]3 {0}", e.ErrorCode);
                        return null;
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("[Server][Receive]4 ", e.ToString());
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

            }
            catch (SocketException e)
            {
                Console.WriteLine("[Server][Send] FrontEnd({0}) socket error", socket.RemoteEndPoint.ToString());
                Console.WriteLine(e.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("[Server][Send] FrontEnd({0}) error", socket.RemoteEndPoint.ToString());
                Console.WriteLine(e.ToString());
            }
        }
        #endregion

        private void CloseSocket(Socket socket)
        {
            Console.WriteLine("[Server][Close] FrontEnd({0}) ", socket.RemoteEndPoint.ToString());
            feSocketList.Remove(socket.RemoteEndPoint.ToString());
            heartBeatList.Remove(socket);
            string ip = socket.RemoteEndPoint.ToString().Split(':')[0];
            int port = int.Parse(socket.RemoteEndPoint.ToString().Split(':')[1]);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());
            redis.DelFEInfo(ip, port);
            redis.DelFEList(ip, port);
            redis.DelFEServiceInfo(feName);
            redis.DelUserLoginKey(feName);
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

                //SIGNIN = 200;
                case Command.SIGNIN:
                    SignIn(packet, socket);
                    break;

                //DUMMY_SIGNIN = 220;
                case Command.DUMMY_SIGNIN:
                    break;

                //SIGNOUT = 300;
                case Command.SIGNOUT:
                    SignOut(packet, socket);
                    break;

                //ROOM_LIST = 400;
                case Command.ROOM_LIST:
                    RoomList(packet, socket);
                    break;

                //CREATE_ROOM = 500;
                case Command.CREATE_ROOM:
                    CreateRoom(packet, socket);
                    break;

                //JOIN = 600;
                case Command.JOIN:
                    JoinRoom(packet, socket);
                    break;

                //    INITIALIZE = 650; 
                case Command.CONNECTION_PASS_SUCCESS:
                    ConnectPass_Succ(packet, socket);
                    break;

                //LEAVE_ROOM = 700;
                case Command.LEAVE_ROOM:
                    LeaveRoom(packet, socket);
                    break;

                //DESTROY_ROOM = 800;
                case Command.DESTROY_ROOM:
                    DestroyRoom(packet, socket);
                    break;

                //MSG = 900;
                case Command.MSG:
                    MSG(packet, socket);
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

        private void Advertise(Packet packet, Socket socket)
        {
            Console.WriteLine("[Server][Advertise]FE({0})", socket.RemoteEndPoint.ToString());

            Header header = packet.header;

            FBAdvertiseRequest advertiseReq = (FBAdvertiseRequest)mc.ByteToStructure(packet.data, typeof(FBAdvertiseRequest));



            string remoteIP = new string(advertiseReq.ip).Split('\0')[0];
            int remotePort = advertiseReq.port;
            string remote = socket.RemoteEndPoint.ToString() ;

            string feName = redis.GetFEName(remote);
            redis.AddFEServiceInfo(feName, remoteIP, remotePort);

        }

        private string MakeCookie(string id, string password)
        {
            DateTime d = new DateTime();
            StringBuilder data = new StringBuilder();
            data.Append(id);
            data.Append(password);
            data.Append(d.Date);

            SHA256 sha = new SHA256Managed();
            byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes(d.ToString()));

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
                //redis.SetUserLogin
                redis.DelUserNumIdCache(usr);
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

        //SIGNIN = 200;
        public void SignIn(Packet packet, Socket socket)
        {
            Header header = packet.header;
            FBSigninRequest loginRequest = (FBSigninRequest)mc.ByteToStructure(packet.data, typeof(FBSigninRequest));

            Packet sendPacket = new Packet();
            bool signin = mysql.Login(new string(loginRequest.user).Split('\0')[0], new string(loginRequest.password).Split('\0')[0]);
            bool duplogin = false;

            if (signin)
            {
                int id = mysql.GetUserID(new string(loginRequest.user).Split('\0')[0]);
                
                string[] list = (string[])redis.GetFEIpPortList();

                foreach (string fe in list)
                {
                    string feName = redis.GetFEName(fe);
                    if (redis.GetUserLogin(feName, id))
                    {
                        sendPacket.data = null;
                        sendPacket.header.code = Command.SIGNIN_FAIL;
                        sendPacket.header.size = 0;
                        sendPacket.header.uid = header.uid;
                        Send(socket, sendPacket);
                        duplogin = true;
                        break;
                    }
                }
            }
            
           if(!duplogin)
            {
                
                if (signin)
                {
                    string cookie = MakeCookie(new string(loginRequest.user), new string(loginRequest.password));


                    Random r = new Random();
                    string[] feList = (string[])redis.GetFEIpPortList();
                    if (feList.Length > 0)
                    {
                        string newFE = feList[r.Next(0, feList.Length)];
                        string feName = redis.GetFEName(newFE);
                        FEInfo feInfo = (FEInfo)redis.GetFEServiceInfo(feName);
                        string newIP = newFE.Split(':')[0];
                        int newPort = int.Parse(newFE.Split(':')[1]);

                        FBConnectionPassRequest initReq = new FBConnectionPassRequest(cookie);
                        sendPacket.data = mc.StructureToByte(initReq);
                        sendPacket.header.code = Command.CONNECTION_PASS;
                        sendPacket.header.size = (ushort)sendPacket.data.Length;
                        sendPacket.header.uid = mysql.GetUserID(new string(loginRequest.user).Split('\0')[0]);

                        Socket fesocket = feSocketList[newFE];
                        Send(fesocket, sendPacket);
                        Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
                        Thread.Sleep(2000);
                        FBSigninResponse LoginResponse = new FBSigninResponse(feInfo.Ip, feInfo.Port, cookie);
                        sendPacket.data = mc.StructureToByte(LoginResponse);
                        sendPacket.header.code = Command.SIGNIN_SUCCESS;
                        sendPacket.header.size = (ushort)sendPacket.data.Length;
                        sendPacket.header.uid = header.uid;
                        Send(socket, sendPacket);
                        Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
                    }
                    else
                    {
                        sendPacket.header.code = Command.SIGNIN_FAIL;
                        sendPacket.header.uid = header.uid;
                        sendPacket.header.size = 0;
                        sendPacket.data = null;

                        Send(socket, sendPacket);
                        Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
                    }
                }
                else
                {
                    sendPacket.header.code = Command.SIGNIN_FAIL;
                    sendPacket.header.size = 0;
                    sendPacket.data = null;

                    Send(socket, sendPacket);
                    Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
                }
            } 
           
        }

        //DUMMY_SIGNIN = 220;
        public void DummySignIn(Packet packet, Socket socket)
        {
           
        }

        //INITIALIZE = 250; 
        public void ConnectPass_Succ(Packet packet, Socket socket)
        {
            Header header = packet.header;
            string username = mysql.GetUserNamebyID(header.uid);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());
            redis.AddUserNumIdCache(username, header.uid);
            redis.SetUserLogin(feName, header.uid, true);
            bool isDummy = mysql.GetUserTypebyID(header.uid);
            redis.SetUserType(username, isDummy);
        }

        //SIGNOUT = 300;
        public void SignOut(Packet packet, Socket socket)
        {
            Header header = packet.header;
            string username = mysql.GetUserNamebyID(header.uid);

            int userID = redis.GetUserNumIdCache(username);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());

           
            if(redis.SetUserLogin(feName, (long)userID, false))
            {
                Console.WriteLine("[Server][SignOut]{0} Success",username);
            }
            else
            {
                Console.WriteLine("[Server][SignOut]{0} Fail", username);
            }
           
                
        }


        //ROOM_LIST = 400;
        public void RoomList(Packet packet, Socket socket)
        {
            string[] feList = (string[])redis.GetFEIpPortList();

            int[] chatRoomList = null;
            foreach (string fe in feList)
            {
                string feName = redis.GetFEName(fe);
                if (chatRoomList != null)
                    chatRoomList = chatRoomList.Concat((int[])redis.GetFEChattingRoomList(feName)).ToArray();
                else
                    chatRoomList = (int[])redis.GetFEChattingRoomList(feName);
            }

            // generate body data
            byte[] data = chatRoomList.SelectMany(BitConverter.GetBytes).ToArray();
            Header header = new Header(Command.ROOM_LIST_SUCCESS, (ushort)data.Length, packet.header.uid);
            Packet sendPacket = new Packet(header, data);

            Send(socket, sendPacket);
            Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
        }

        //CREATE_ROOM = 500;
        public void CreateRoom(Packet packet, Socket socket)
        {
            Header header = packet.header;
            string username = mysql.GetUserNamebyID(header.uid);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());
            int result = redis.CreateChatRoom(feName, username);

            Console.WriteLine("[Server][Create Room] FE({0}) create Room#{1}", feName, result);

            Packet sendPacket = new Packet();                                           
            FBRoomCreateResponse createRes = new FBRoomCreateResponse(result);
            sendPacket.data = mc.StructureToByte(createRes);
            sendPacket.header.code = Command.CREATE_ROOM_SUCCESS;
            sendPacket.header.uid = header.uid;
            sendPacket.header.size = (ushort)sendPacket.data.Length;
            
                                                                                                                                                                                                                                                                                                                                                                                                                
            Send(socket, sendPacket);
            Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);
        }

        //JOIN = 600;
        public void JoinRoom(Packet packet, Socket socket)
        {
            Header header = packet.header;
            FBRoomJoinRequest joinReq = (FBRoomJoinRequest)mc.ByteToStructure(packet.data, typeof(FBRoomJoinRequest));

            string username = mysql.GetUserNamebyID(header.uid);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());

            Packet sendPacket = new Packet();

            bool joinResfalg = false;

            if (redis.HasChatRoom(feName, joinReq.roomNum))
            {
                if(redis.GetChatRoomCount(feName, joinReq.roomNum) > 0)
                {
                    sendPacket.header.code = Command.JOIN_FULL_FAIL;
                    sendPacket.header.uid = header.uid;
                    sendPacket.header.size = 0;
                    sendPacket.data = null;
                    Console.WriteLine("[Server][Join Room] FE({0}) join Room#{1} Fail : Full", feName, joinReq.roomNum);
                    
                }
                else
                {
                    redis.AddUserChatRoom(feName, joinReq.roomNum, username);
                    redis.IncChatRoomCount(feName, joinReq.roomNum);

                    sendPacket.header.code = Command.JOIN_SUCCESS;
                    sendPacket.header.uid = header.uid;
                    sendPacket.header.size = 0;
                    sendPacket.data = null;

                    Console.WriteLine("[Server][Join Room] FE({0}) join Room#{1}", feName, joinReq.roomNum);
                }

                joinResfalg = true;
            }
            else
            {
                string[] felist = (string[])redis.GetFEIpPortList();

                foreach (string feIpPort in felist)
                {
                    string fe = redis.GetFEName(feIpPort);

                    if (redis.HasChatRoom(fe, joinReq.roomNum))
                    {
                        if (redis.GetChatRoomCount(feName, joinReq.roomNum) > 0)
                        {
                            sendPacket.header.code = Command.JOIN_FULL_FAIL;
                            sendPacket.header.uid = header.uid;
                            sendPacket.header.size = 0;
                            sendPacket.data = null;
                            joinResfalg = true;
                            Console.WriteLine("[Server][Join Room] FE({0}) join Room#{1} Fail : Full", feName, joinReq.roomNum);
                        }
                        else
                        {

                            redis.AddUserChatRoom(feName, joinReq.roomNum, username);
                            redis.IncChatRoomCount(feName, joinReq.roomNum);

                            FEInfo info = (FEInfo)redis.GetFEServiceInfo(fe);

                            string password = mysql.GetPasswordID(header.uid);
                            string cookie = MakeCookie(username, password);
                            FBConnectionPassRequest connPassReq = new FBConnectionPassRequest(cookie);
                            sendPacket.data = mc.StructureToByte(connPassReq);
                            sendPacket.header.code = Command.CONNECTION_PASS;
                            sendPacket.header.uid = header.uid;
                            sendPacket.header.size = (ushort)sendPacket.data.Length;
                           
                            
                            Send(feNameSocketList[info.Name], sendPacket);
                            Console.WriteLine("[Server][Redirect] send to FE({0}) ", feNameSocketList[info.Name]);

                            FBRoomJoinRedirectResponse joinRes = new FBRoomJoinRedirectResponse(info.Ip.ToCharArray(), info.Port, cookie.ToCharArray());
                            sendPacket.data = mc.StructureToByte(joinRes);
                            sendPacket.header.code = Command.JOIN_REDIRECT;
                            sendPacket.header.uid = header.uid;
                            sendPacket.header.size = (ushort)sendPacket.data.Length;
                            Console.WriteLine("[Server][Join Room] FE({0}) doesn't have Room#{1} redirect to {2}", feName, joinReq.roomNum, fe);

                            joinResfalg = true;
                            break;
                        }
                        joinResfalg = true;

                    }
                    
                }

            }

            if (!joinResfalg)
            {
                sendPacket.header.code = Command.JOIN_NULL_FAIL;
                sendPacket.header.uid = header.uid;
                sendPacket.header.size = 0;
                sendPacket.data = null;
                Console.WriteLine("[Server][Join Room] FE({0}) join Room#{1} Fail : null", feName, joinReq.roomNum);
            }
            
            Send(socket, sendPacket);
            Console.WriteLine("[Server][Send] FrontEnd({0}) {1}", socket.RemoteEndPoint.ToString(), sendPacket.header.code);

        }
        
        //LEAVE_ROOM = 700;
        private void LeaveRoom(Packet packet, Socket socket)
        {
            Header header = packet.header;
            FBRoomLeaveRequest leaveReq = (FBRoomLeaveRequest)mc.ByteToStructure(packet.data, typeof(FBRoomLeaveRequest));

            string username = mysql.GetUserNamebyID(header.uid);
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());

            bool leaveResult = redis.LeaveChatRoom(feName, leaveReq.roomNum, username);
            int count = redis.DecChatRoomCount(feName, leaveReq.roomNum);

            Packet sendPacket = new Packet();
            sendPacket.header.uid = header.uid;
            sendPacket.header.code = Command.LEAVE_ROOM_SUCCESS;
            sendPacket.header.size = 0;
            sendPacket.data = null;
                        
            Send(socket, sendPacket);
            Console.WriteLine("[Server][Leave]{0} leave Room#{1} Success", username, leaveReq.roomNum);
        }
        

        //DESTROY_ROOM = 800;
        private void DestroyRoom(Packet packet, Socket socket)
        {
            Header header = packet.header;
            
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());
            FBRoomDestroyRequest destroyReq = (FBRoomDestroyRequest)mc.ByteToStructure(packet.data, typeof(FBRoomDestroyRequest));
            int room = destroyReq.roomNum;

            redis.DelFEChattingRoom(feName, room);

            Console.WriteLine("[Server][Destroy]{0} destroy Room#{1}", feName, room);
        }

        //MSG = 900;
        private void MSG(Packet packet, Socket socket)
        {
            Header header = packet.header;
            string userName = mysql.GetUserNamebyID(header.uid);

            bool isDummy = redis.GetUserType(userName);
            if (isDummy)
            {
                Console.WriteLine("[Server][Chat] dummy({0})", userName);
            }
            else
            {
                redis.AddChat(userName);
                Console.WriteLine("[Server][Chat] {0}", userName);
            }
        }

        //HEARTBEAT = 1000;
        private void ReceiveHeartBeat(Packet packet, Socket socket)
        {
            //Console.WriteLine("[Server][Receive]Heartbeat from FE({0})", socket.RemoteEndPoint.ToString());
            heartBeatList[socket] = 0;
            Packet sendPacket = new Packet();
            sendPacket.data = null;
            sendPacket.header.uid = 0;
            sendPacket.header.code = Command.HEARTBEAT_SUCCESS;
            sendPacket.header.size = 0;
            Send(socket, sendPacket);
        }
    }


}
