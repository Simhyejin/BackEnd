using System;
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

        private ConnectionMultiplexer redisdb = null;
        private IDatabase db = null;
        private ISubscriber pubsub = null;

        private static RedisController redisInstance = null;


        public static string FE = "fe";
        public static char DELIMITER = ':';

        public static string FE_List = "fe:list";
        public static string Login = "login";
        public static string RoomList = "roomlist";

        public static string Room = "room";
        public static string Count = "count";

        public static string User = "user";
        public static string Dummy = "dummy";

        public static string Ranking_Chatting = "ranking:chatting";

        public static string FEServiceInfo = "serviceinfo";

        public static int feNo = 0;
        public static int roomNo = 0;


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

        public ConnectionMultiplexer Redis
        {
            get { return this.redisdb; }
        }

        
        public bool Connect()
        {
            try
            {
                redisdb = ConnectionMultiplexer.Connect("192.168.56.110:6379" + ",allowAdmin=true,password=433redis!");
                pubsub = redisdb.GetSubscriber();
                db = redisdb.GetDatabase();
                Console.WriteLine("[ Redis ][ Connect ] Success");
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public string AddFEConnectedInfo(string ip, int port)
        {
            string key = ip + DELIMITER + port;
            string feName = "fe" + GenerateFEName();
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
            FrontEnd feService = new FrontEnd();
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
            string key = feName + DELIMITER + RoomList;
            return db.SetContains(key, roomNo);
        }

        public bool AddFEList(string ip, int port)
        {
            string key = "fe:list";
            string value = ip + DELIMITER + port;
            return db.SetAdd(key, value);
        }

       
        public object GetFEAddressList()
        {
            string KEY = "fe:list";
            string[] feList = null;

            RedisValue[] result = db.SetMembers(KEY);
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

        
        public bool SetUserLogin(string remoteName, long userNumId, bool state)
        {
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(DELIMITER);
            sb.Append(login);

            string key = sb.ToString();

            return db.StringSetBit(key, userNumId, state);
        }

        public bool GetUserLogin(string remoteName, long userNumId)
        {
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(DELIMITER);
            sb.Append(login);

            string key = sb.ToString();

            return db.StringGetBit(key, userNumId);
        }

        public bool DelUserLoginKey(string remoteName)
        {
            string login = "login";
            StringBuilder sb = new StringBuilder();

            sb.Append(remoteName);
            sb.Append(DELIMITER);
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
            int numId = (int)db.StringGet(id);
            int roomNo = GenerateRoomNo();

            StringBuilder sb = new StringBuilder();

            sb.Append(feName);
            sb.Append(DELIMITER);
            sb.Append(RoomList);

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
        

        public bool DelFEChattingRoomListKey(string feName)
        {
            string key = feName + DELIMITER + RoomList;

            return db.KeyDelete(key);
        }

        public bool DelFEChattingRoom(string feName, int roomNo)
        {
            string key = feName + DELIMITER + RoomList;

            return db.SetRemove(key, roomNo);
        }


        public object GetFEChattingRoomList(string feName)
        {
            int[] roomList = null;

            StringBuilder sb = new StringBuilder();
           
            sb.Append(feName);
            sb.Append(DELIMITER);
            sb.Append(RoomList);

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
            
            SortedSetEntry[] ranks = db.SortedSetRangeByRankWithScores(Ranking_Chatting, 0, range, Order.Descending);
            return ranks;
        }

        
        public void ClearDB()
        {
            if (Redis != null)
                redisdb.GetServer(REDIS_IP, REDIS_PORT).FlushDatabase();
        }

        public static int GenerateFEName()
        {
            Interlocked.Increment(ref feNo);
            return feNo;
        }

        public static int GenerateRoomNo()
        {
            Interlocked.Increment(ref roomNo);
            return roomNo;
        }

    }

}