using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Login
{
    

    class RedisHandlerForLogin
    {
        const string REDIS_IP = "10.100.58.4";
        //const string REDIS_IP = "192.168.56.110";
        const int REDIS_PORT = 36379;

        Redis redis = null;

        public RedisHandlerForLogin()
        {
            
            redis = new Redis(new ConfigurationOptions{
                                    EndPoints =
                                            {
                                                { REDIS_IP, REDIS_PORT }
                                            },
                                    KeepAlive = 180,
                                    Password = "433redis!",
                                    AllowAdmin = true
                                     });
            ClearDB();
        }


        public void ClearDB()
        {
            redis.Connection.GetServer(REDIS_IP,REDIS_PORT).FlushAllDatabases();
        }


        public void CloseSocket(Socket socket)
        {
            string feName = redis.GetFEName(socket.RemoteEndPoint.ToString());
            string ip = socket.RemoteEndPoint.ToString().Split(':')[0];
            int port = int.Parse(socket.RemoteEndPoint.ToString().Split(':')[1]);
            redis.DelFEInfo(ip, port);
            redis.DelFEList(ip, port);
            redis.DelFEInfoForClient(feName);
            redis.DelUserLoginFE(feName);
            redis.DelFERoomList(feName);

        }

        public string AcceptFE(string ip, int port)
        {
            bool result = redis.AddFEList(ip, port);

            return redis.AddFEInfo(ip, port);
        }

        public void DeleteUser(string feipport, long userid, string username)
        {
            string feName = redis.GetFEName(feipport);
            redis.SetUserLogin(feName, userid, false);
            redis.DelUser(username);
        }

        public bool DupplicateSignIn(long userid)
        {
            string[] list = (string[])redis.GetFEList();

            foreach (string fe in list)
            {
                string feName = redis.GetFEName(fe);
                if (redis.GetUserLogin(feName, userid))
                {
                   return  true;
                }
            }
            return false;
        }

        public string GetRamdomFE(ref string ip, ref int port)
        {
            Random r = new Random();
            string[] feList = (string[])redis.GetFEList();
            string newFE = null;
            if (feList.Length > 0)
            {
                newFE  = feList[r.Next(0, feList.Length)];
                string feName = redis.GetFEName(newFE);
                FrontEnd feInfo = (FrontEnd)redis.GetFEInfoForClient(feName);
                ip = feInfo.Ip;
                port = feInfo.Port;
            }

            return newFE;
        }

        public void ConnectPassSuccess(string feAddress, string username, long userid, bool isDummy)
        {
            string feName = redis.GetFEName(feAddress);
            redis.AddUser(username, userid);
            redis.AddUserList(username);
            redis.SetUserLogin(feName, userid, true);
            redis.SetUserType(username, isDummy);
        }


        public void AddFEinfoForClient(string feAddress, string ip, int port)
        {
            string feName = redis.GetFEName(feAddress);
            redis.AddFEInfoForClient(feName, ip, port);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="username"></param>
        /// <param name="roomNo"></param>
        /// <param name="newFE"></param>
        /// <returns> -1 : doesn't exist room  0 : success 1 : full room </returns>
        public int GetFEinfoForClient(string username, int roomNo, out FrontEnd newFE)
        {
            string[] felist = (string[])redis.GetFEList();
            newFE = null;
            int result = -1;
            foreach (string feIpPort in felist)
            {
                string fe = redis.GetFEName(feIpPort);

                if (redis.HasRoom(fe, roomNo))
                {
                    if (redis.GetRoomCount(fe, roomNo) > 10)
                    {
                        result = 1;
                    }
                    else
                    {
                        redis.AddUserRoom(fe, roomNo, username);
                        newFE = (FrontEnd)redis.GetFEInfoForClient(fe);

                        result = 0;
                    }
                }
            }
            return result;
        }
    }
}
