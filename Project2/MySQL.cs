using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;

namespace BackEnd
{
    class MySQL
    {
        MySqlConnection conn = null;

        public MySQL()
        {
            
        }

        public bool Connect()
        {
            try
            {
                conn = new MySqlConnection();
                conn.ConnectionString = "server=192.168.56.120;uid=root;pwd=433intern;database=BE;Charset=utf8";
                conn.Open();
                Console.WriteLine("[ MYSQL ][ Connect ] Success");
                INIT();
                return true;
            }
           catch(MySqlException)
            {
                Console.WriteLine("[ MYSQL ][ Connect ] Fail");
                return false;
            }
            catch (Exception)
            {
                Console.WriteLine("[ MYSQL ][ Connect ] Fail");
                return false;
            }
            
        }

        /// <summary>
        /// Create Users Table
        /// </summary>
        private void INIT()
        {
            try
            {
                StringBuilder createQuery = new StringBuilder();
                createQuery.Append("CREATE TABLE IF NOT EXISTS Users (");
                createQuery.Append("USER_ID INT NOT NULL AUTO_INCREMENT,");
                createQuery.Append("USER_NAME VARCHAR(20) NOT NULL,");
                createQuery.Append("PASSWORD VARCHAR(20) NOT NULL,");
                createQuery.Append("DUMMY BOOL NOT NULL,");
                createQuery.Append("CREATE_TIME DATETIME NOT NULL,");
                createQuery.Append("PRIMARY KEY(user_id),");
                createQuery.Append("UNIQUE INDEX(user_name))");

                MySqlCommand command = new MySqlCommand(createQuery.ToString(), conn);
                command.ExecuteNonQuery();
                
                Console.WriteLine("[ MYSQL ][ Create ] Table Success");

            }
            catch(Exception)
            {
                Console.WriteLine("[ MYSQL ][ Create ] Table FAIL");
            }
             
        }

        /// <summary>
        /// Sign Up
        /// Insert user into Users table
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <param name="isDummy"></param>
        public bool InsertUser(string user, string password, bool isDummy)
        {
            try
            {
                StringBuilder insertQuery = new StringBuilder();
                insertQuery.Append("INSERT INTO Users (USER_NAME, PASSWORD, DUMMY, CREATE_TIME) VALUES (");
                insertQuery.Append("'" + user + "', ");
                insertQuery.Append("'" + password + "', ");
                insertQuery.Append(isDummy + ", ");
                insertQuery.Append("now())");
                
                MySqlCommand command = new MySqlCommand(insertQuery.ToString(), conn);
                command.ExecuteNonQuery();

                Console.WriteLine("[ MYSQL ][ Insert ] User({0}) Success", user);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[ MYSQL ][ Insert ] User({0}) FAIL", user);

                Console.WriteLine(e.ToString());
                return false;
            }
            
        }

        /// <summary>
        /// Sign Out
        /// Delete User from Users table
        /// </summary>
        /// <param name="user"></param>
        public bool DeleteUser(string user)
        {
            try
            {
                StringBuilder deleteQuery = new StringBuilder();
                deleteQuery.Append("DELETE FROM Users ");
                deleteQuery.Append("WHERE USER_NAME = ");
                deleteQuery.Append("'" + user + "'");

                MySqlCommand command = new MySqlCommand(deleteQuery.ToString(), conn);
                command.ExecuteNonQuery();

                Console.WriteLine("[ MYSQL ][ Delete ] User({0}) Success", user);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("[ MYSQL ][ Delete ] User({0}) FAIL", user);

                Console.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// check id wherer if it can be used
        /// </summary>
        /// <param name="usr"></param>
        /// <returns></returns>
        public bool CheckDupID(string usr)
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT count(*) FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + usr + "'");

                MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
                int queryResult = int.Parse(command.ExecuteScalar().ToString());

                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Success for dup id", usr);

                if (queryResult == 0)
                {
                    Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) is not used", usr);
                    return true;
                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) is already used", usr);
                    return false;
                }
            }
            catch (MySqlException e)
            {
                Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) MYSQLEXCEPTION", usr);

                Console.WriteLine(e.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) UNHANDLED EXCEPTION", usr);

                Console.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Check id, password in Users table 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Login(string user, string password)
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT count(*) FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + user + "'");
                selectQuery.Append("AND PASSWORD = ");
                selectQuery.Append("'" + password + "'");


                MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
                int queryResult = int.Parse(command.ExecuteScalar().ToString());

                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Success for Login", user);

                if (queryResult == 1)
                {
                    Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) Success", user);
                    return true;
                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) Fail", user);
                    return false;
                }
            }
            catch (MySqlException e)
            {
                
                Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) MYSQLEXCEPTION", user);
                Console.WriteLine(e.ToString());
                return false;
            }
            catch (Exception e)
            {
                Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) UNHANDLED EXCEPTION", user);

                Console.WriteLine(e.ToString());
                return false;
            }
        }

        /// <summary>
        /// Update Password from Users table 
        /// </summary>
        /// <param name="user"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public bool UpdatePassword(string user, string oldPassword, string newPassword) 
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT PASSWORD FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + user + "'");
                
                MySqlCommand command  = new MySqlCommand(selectQuery.ToString(), conn);
                string queryResult = command.ExecuteScalar().ToString();

                Console.WriteLine(queryResult);
                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Password Success", user);


                if (oldPassword == queryResult)
                {
                    StringBuilder updateQuery = new StringBuilder();
                    updateQuery.Append("UPDATE Users SET PASSWORD = ");
                    updateQuery.Append("'" + newPassword + "' ");
                    updateQuery.Append("WHERE USER_NAME = '"+user+"'");

                    command = new MySqlCommand(updateQuery.ToString(), conn);
                    command.ExecuteNonQuery();

                    Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) Password Success", user);

                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) Wrong Password", user);

                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) FAIL", user);
                return false;
            }
        }
        public bool UpdatePassword(string user, string newPassword)
        {
            try
            {
               
                StringBuilder updateQuery = new StringBuilder();
                updateQuery.Append("UPDATE Users SET PASSWORD = ");
                updateQuery.Append("'" + newPassword + "' ");
                updateQuery.Append("WHERE USER_NAME = '" + user + "'");

                MySqlCommand command = new MySqlCommand(updateQuery.ToString(), conn);
                command.ExecuteNonQuery();

                Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) Password Success", user);

               

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) FAIL", user);
                return false;
            }
        }

        public int GetUserID(string user)

        {
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT USER_ID FROM Users ");
            selectQuery.Append("WHERE USER_NAME = ");
            selectQuery.Append("'" + user + "'");

            MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);

            return int.Parse(command.ExecuteScalar().ToString());

        }

        public string GetUserNamebyID(long id)
        {
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT USER_NAME FROM Users ");
            selectQuery.Append("WHERE USER_ID = ");
            selectQuery.Append("'" + id + "'");

            MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
            return command.ExecuteScalar().ToString();

        }

        public string GetPasswordID(long id)
        {
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT PASSWORD FROM Users ");
            selectQuery.Append("WHERE USER_ID = ");
            selectQuery.Append("'" + id + "'");

            MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
            return command.ExecuteScalar().ToString();

        }

        public bool GetUserTypebyID(long id)
        {
            StringBuilder selectQuery = new StringBuilder();
            selectQuery.Append("SELECT DUMMY FROM Users ");
            selectQuery.Append("WHERE USER_ID = ");
            selectQuery.Append("'" + id + "'");

            MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
            return (bool)command.ExecuteScalar();
        }

    }
}
