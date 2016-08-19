using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static BackEnd.Packet;
using MikRedisDB;

namespace BackEnd
{
    class BackEnd
    {
        private Socket listenSocket;
        private Dictionary<string, Socket> frontEndList= null;

        private MConvert mc = new MConvert();
        private const int HEAD_SIZE = 4;

        private int backlog = 10;
        private bool listening;
        private int port;

        private MySQL mysql;
        private RedisDBController redis;


        Task<Socket> acceptTask=null;

        public BackEnd(int port)
        {
            mysql = new MySQL();
            this.port = port;
            listening = true;
        }

        public async void Start()
        {
            await Task.WhenAll(BindListenerAsync(port), ConnectMySQLAsync(), ConnectRedisAsync());
            while (listening)
            {
                AcceptClient();
            }
            
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

        public Task ConnectMySQLAsync()
        {
            return Task.Run(() => {
                mysql.Connect(); 
            });
        }

        public Task ConnectRedisAsync()
        {
            return Task.Run(() => {
                RedisSet();
            });
        }


        private void RedisSet()
        {
            //Connect with Redis DB
            
            redis = new RedisDBController();
            try
            {
                redis.SetConfigurationOptions("192.168.56.110", 6379, "433redis!");
                redis.SetupConnection();
                
            }
            catch (Exception)
            {
                Console.WriteLine("[ Redis ][ Connect ] Fail");
            }
           
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
                        Socket frontEnd = await AcceptAsync();
                        Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", frontEnd.RemoteEndPoint.ToString());
                        Receive(frontEnd);
                    }
                }
                else
                {
                    Socket frontEnd = await AcceptAsync();
                    Console.WriteLine("[Server][Accept]  FrontEnd({0}) is Connected.", frontEnd.RemoteEndPoint.ToString());
                    Receive(frontEnd);
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
                    socket= listenSocket.Accept();
                }
                catch (Exception)
                {
                    socket = null;
                    Console.WriteLine("err");
                }
                
                return socket;
            } );
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
    }


}
