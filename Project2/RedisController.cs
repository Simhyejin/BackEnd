﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using StackExchange.Redis;
using System.Threading;

namespace BackEnd
{
    /*****
     * 
     * 함수 명 다시 ...
     */
    public class RedisController
    {
        const string REDIS_IP = "192.168.56.110";
        const int REDIS_PORT = 6379;
        private ConnectionMultiplexer _redis = null;
        private IDatabase db = null;
        private ISubscriber pubsub = null;
        private static RedisController redisInstance = null;


        public static string FE = "fe";
        public static char DELIMITER = ':';

        public static string FE_List = "fe:list";
        public static string Login = "login";
        public static string ChattingRoomList = "chattingroomlist";

        public static string Room = "room";
        public static string Count = "count";

        public static string User = "user";

        public static string Ranking_Chatting = "ranking:chatting";

        public static string FEServiceInfo = "serviceinfo";

        public static string Dummy = "dummy";

        
        private RedisController() { }
        public static RedisController RedisInstance
        {
            get
            {
                if (redisInstance == null)
                {
                    redisInstance = new RedisController();
                    redisInstance.Connect();
                }
                return redisInstance;
            }
        }


        public void Start() { }

        public ConnectionMultiplexer Redis
        {
            get { return this._redis; }
        }

        
        public bool Connect()
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect("192.168.56.110:6379" + ",allowAdmin=true,password=433redis!");
                pubsub = _redis.GetSubscriber();
                db = _redis.GetDatabase();
                Console.WriteLine("[ Redis ][ Connect ] Success");
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

        
        public FEInfo GetFEInfo(string feName)
        {
            // key : 
            HashEntry[] feInfo = db.HashGetAll(feName);

            FEInfo fe = new FEInfo();
            for (int idx = 0; idx < feInfo.Length; idx++)
            {
                // name
                if (feInfo[idx].Name.Equals("name"))
                {
                    fe.Name = feInfo[idx].Value;
                }
                else if (feInfo[idx].Name.Equals("ip"))
                {
                    // ip 
                    fe.Ip = feInfo[idx].Value;
                }
                else
                {
                    //prot
                    fe.Port = Int32.Parse(feInfo[idx].Value);
                }

            }
            return fe;
        }

        public string AddFEConnectedInfo(string ip, int port)
        {
            string key = ip + DELIMITER + port;
            string feName = "fe" + FENameGenerator.GenerateName();
            if (db.StringSet(key, feName))
                return feName;
            return "";
        }

        public bool DelFEInfo(string ip, int port)
        {
            string key = ip + DELIMITER + port;
            return db.KeyDelete(key);
        }

        public void AddFEServiceInfo(string feName, string ip, int port)
        {
            string key = feName + DELIMITER + FEServiceInfo;

            HashEntry[] entries = new HashEntry[2];
            entries[0] = new HashEntry("ip", ip);
            entries[1] = new HashEntry("port", port);

            db.HashSet(key, entries);
        }

        public object GetFEServiceInfo(string feName)
        {
            string key = feName + DELIMITER + FEServiceInfo;

            HashEntry[] feServiceInfo = db.HashGetAll(key);
            FEInfo feService = new FEInfo();
            feService.Name = feName;
            foreach (HashEntry info in feServiceInfo)
            {
                if (info.Name.Equals("ip"))
                    feService.Ip = info.Value;
                else
                    feService.Port = (int)info.Value;
            }

            return feService;
        }

        public bool DelFEServiceInfo(string feName)
        {
            string key = feName + DELIMITER + FEServiceInfo;
            return db.KeyDelete(key);
        }

       
        public bool HasChatRoom(string feName, int roomNo)
        {
            string key = feName + DELIMITER + ChattingRoomList;
            return db.SetContains(key, roomNo);
        }

        public bool AddFEList(string ip, int port)
        {
            string key = "fe:list";
            string value = ip + DELIMITER + port;
            return db.SetAdd(key, value);
        }

       
        public object GetFEIpPortList()
        {
            string KEY = "fe:list";
            string[] feList = null;

            RedisValue[] result = db.SetMembers(KEY);

            // result => ip:port 임
            feList = new string[result.Length];
            for (int idx = 0; idx < result.Length; idx++)
            {
                feList[idx] = result[idx];
            }

            return feList;
        }

       
        public bool DelFEList(string ip, int port)
        {
            string key = "fe:list";
            string value = ip + DELIMITER + port;
            return db.SetRemove(key, value);
        }

        
        public bool AddUserNumIdCache(string key, long value)
        {
            return db.StringSet(key, value);
        }

        public int GetUserNumIdCache(string key)
        {
            return (int)db.StringGet(key);
        }

        public bool DelUserNumIdCache(string key)
        {
            return db.KeyDelete(key);
        }

        
        public string GetFEName(String feIpPort)
        {
            string key = feIpPort;

            string feName = db.StringGet(key);
            return feName;
        }

        public int GetFETotalChatRoomCount(string feName)
        {
            string key = feName + DELIMITER + ChattingRoomList;
            int roomCount = (int)db.SetLength(key);
            return roomCount;
        }

        
        public bool SetUserLogin(string remoteName, long userNumId, bool state)
        {
            string delimiter = ":";
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(delimiter);
            sb.Append(login);

            string key = sb.ToString();

            return db.StringSetBit(key, userNumId, state);
        }

        public bool GetUserLogin(string remoteName, long userNumId)
        {
            string delimiter = ":";
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(delimiter);
            sb.Append(login);

            string key = sb.ToString();

            return db.StringGetBit(key, userNumId);
        }

        public bool DelUserLoginKey(string remoteName)
        {
            string delimiter = ":";
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(delimiter);
            sb.Append(login);

            string key = sb.ToString();

            return db.KeyDelete(key);
        }

        public bool SetUserType(string id, bool userType)
        {
            int userNumId = GetUserNumIdCache(id);

            return db.StringSetBit(Dummy, userNumId, userType);
        }

       
        public bool GetUserType(string id)
        {
            int userNumId = GetUserNumIdCache(id);

            return db.StringGetBit(Dummy, userNumId);
        }

        
        public int CreateChatRoom(string feName, string id)
        {
            StringBuilder sb = new StringBuilder();

            int numId = (int)db.StringGet(id);

         
            int roomNo = ChatRoomNumberGenerator.GenerateRoomNo();

            Console.WriteLine("Generated room number : " + roomNo);

           

            sb.Append(feName);
            sb.Append(DELIMITER);
            sb.Append(ChattingRoomList);

            string key = sb.ToString();

            db.SetAdd(key, roomNo);

            
            NewChatRoomCount(feName, roomNo);

            return roomNo;
        }
        public bool AddUserChatRoom(string feName, int roomNo, string id)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + User;
            return db.SetAdd(key, id);
        }

        
        public bool LeaveChatRoom(string feName, int roomNo, string id)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + User;

            return db.SetRemove(key, id);
        }

        public bool DelUserChatRoomKey(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + User;
            return db.KeyDelete(key);
        }



      
        public int DecChatRoomCount(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + Count;

            return (int)db.StringDecrement(key, 1);
        }

        public bool NewChatRoomCount(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + Count;
            return db.StringSet(key, 0);
        }

        public void IncChatRoomCount(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + Count;
            
        }
        
        public int GetChatRoomCount(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + Count;

            return (int)db.StringGet(key);
        }

        public bool DelChattingRoomCountKey(string feName, int roomNo)
        {
            string key = feName + DELIMITER + Room + roomNo + DELIMITER + Count;
            return db.KeyDelete(key);
        }

        public bool DelFEChattingRoomListKey(string feName)
        {
            string chattingRoomList = "chattingroomlist";
            string key = feName + DELIMITER + chattingRoomList;

            return db.KeyDelete(key);
        }

        public bool DelFEChattingRoom(string feName, int roomNo)
        {
            string chattingRoomList = "chattingroomlist";
            string key = feName + DELIMITER + chattingRoomList;

            return db.SetRemove(key, roomNo);
        }


        public object GetFEChattingRoomList(string feName)
        {
            int[] roomList = null;

            StringBuilder sb = new StringBuilder();
            string chattingRoomList = "chattingroomlist";

            sb.Append(feName);
            sb.Append(DELIMITER);
            sb.Append(chattingRoomList);

            string key = sb.ToString();

            roomList = new int[db.SetLength(key)];

            RedisValue[] values = db.SetMembers(key);

            for (int idx = 0; idx < values.Length; idx++)
            {
                roomList[idx] = (int)values[idx];
            }

            return roomList;
        }

       
        public void AddChat(string id)
        {

            db.SortedSetIncrement(Ranking_Chatting, id, 1);

        }

        public object GetChattingRanking(int range)
        {
            
            RedisValue[] ranks = db.SortedSetRangeByRank(Ranking_Chatting, 0, range, Order.Descending);
            return ranks;
        }

        
        public void ClearDB()
        {
            if (Redis != null)
                _redis.GetServer(REDIS_IP, REDIS_PORT).FlushDatabase();
        }
    }

    public static class FENameGenerator
    {
        public static int num = 0;
        public static int GenerateName()
        {
            Interlocked.Increment(ref num);
            return num;
        }

    }

    public static class ChatRoomNumberGenerator
    {
        public static int roomNo = 0;
        public static int GenerateRoomNo()
        {
            Interlocked.Increment(ref roomNo);
            return roomNo;
        }
    }
}