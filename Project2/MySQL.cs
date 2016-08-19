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
        /// <param name="id"></param>
        /// <param name="password"></param>
        /// <param name="isDummy"></param>
        public bool InsertUser(string id, string password, bool isDummy)
        {
            try
            {
                StringBuilder insertQuery = new StringBuilder();
                insertQuery.Append("INSERT INTO Users (USER_NAME, PASSWORD, DUMMY, CREATE_TIME) VALUES (");
                insertQuery.Append("'" + id + "', ");
                insertQuery.Append("'" + password + "', ");
                insertQuery.Append(isDummy + ", ");
                insertQuery.Append("now())");
                
                MySqlCommand command = new MySqlCommand(insertQuery.ToString(), conn);
                command.ExecuteNonQuery();

                Console.WriteLine("[ MYSQL ][ Insert ] User({0}) Success", id);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("[ MYSQL ][ Insert ] User({0}) FAIL", id);
                return false;
            }
            
        }

        /// <summary>
        /// Sign Out
        /// Delete User from Users table
        /// </summary>
        /// <param name="id"></param>
        public bool DeleteUser(string id)
        {
            try
            {
                StringBuilder deleteQuery = new StringBuilder();
                deleteQuery.Append("DELETE FROM Users ");
                deleteQuery.Append("WHERE USER_NAME = ");
                deleteQuery.Append("'" + id + "'");

                MySqlCommand command = new MySqlCommand(deleteQuery.ToString(), conn);
                command.ExecuteNonQuery();

                Console.WriteLine("[ MYSQL ][ Delete ] User({0}) Success", id);

                return true;
            }
            catch (Exception)
            {
                Console.WriteLine("[ MYSQL ][ Delete ] User({0}) FAIL", id);
                return false;
            }
        }

        /// <summary>
        /// check id wherer if it can be used
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool CheckDupID(string id)
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT count(*) FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + id + "'");

                MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
                int queryResult = (int)command.ExecuteScalar();

                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Success for dup id", id);

                if (queryResult == 0)
                {
                    Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) is not used", id);
                    return true;
                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) is already used", id);
                    return false;
                }
            }
            catch (MySqlException)
            {
                Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) MYSQLEXCEPTION", id);
                return false;
            }
            catch (Exception)
            {
                Console.WriteLine("[ MYSQL ][ DUPID ] User({0}) UNHANDLED EXCEPTION", id);
                return false;
            }
        }

        /// <summary>
        /// Check id, password in Users table 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Login(string id, string password)
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT count(*) FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + id + "'");
                selectQuery.Append("AND PASSWORD = ");
                selectQuery.Append("'" + password + "'");


                MySqlCommand command = new MySqlCommand(selectQuery.ToString(), conn);
                int queryResult = (int)command.ExecuteScalar();

                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Success for Login", id);

                if (queryResult == 1)
                {
                    Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) Success", id);
                    return true;
                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) Fail", id);
                    return false;
                }
            }
            catch (MySqlException)
            {
                Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) MYSQLEXCEPTION", id);
                return false;
            }
            catch (Exception)
            {
                Console.WriteLine("[ MYSQL ][ LOGIN ] User({0}) UNHANDLED EXCEPTION", id);
                return false;
            }
        }

        /// <summary>
        /// Update Password from Users table 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public bool UpdatePassword(string id, string oldPassword, string newPassword) 
        {
            try
            {
                StringBuilder selectQuery = new StringBuilder();
                selectQuery.Append("SELECT PASSWORD FROM Users ");
                selectQuery.Append("WHERE USER_NAME = ");
                selectQuery.Append("'" + id + "'");
                
                MySqlCommand command  = new MySqlCommand(selectQuery.ToString(), conn);
                string queryResult = (string)command.ExecuteScalar();

                Console.WriteLine(queryResult);
                Console.WriteLine("[ MYSQL ][ SELECT ] User({0}) Password Success", id);


                if (oldPassword == queryResult)
                {
                    StringBuilder updateQuery = new StringBuilder();
                    updateQuery.Append("UPDATE Users SET PASSWORD = ");
                    updateQuery.Append("'" + newPassword + "' ");
                    updateQuery.Append("WHERE USER_NAME = '"+id+"'");

                    command = new MySqlCommand(updateQuery.ToString(), conn);
                    command.ExecuteNonQuery();

                    Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) Password Success", id);

                }
                else
                {
                    Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) Wrong Password", id);

                }

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                Console.WriteLine("[ MYSQL ][ UPDATE ] User({0}) FAIL", id);
                return false;
            }
        }
        
    }
}
