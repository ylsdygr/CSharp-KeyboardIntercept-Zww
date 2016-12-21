using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using MySql.Data;
using MySql.Data.MySqlClient;
using System.Data;
using System.Collections;

namespace KeyboardIntercept
{
    public class DatabaseOperator
    {
        public String mysqlInfo = String.Empty;  //数据库连接信息总串
        protected String para_DatabaseIP = String.Empty;  //数据库连接IP
        protected String para_DatabaseUser = String.Empty;  //数据库连接用户名
        protected String para_DatabasePWD = String.Empty;  //数据库连接密码
        protected String para_DatabaseName = String.Empty;  //数据库名称
        protected String para_DatabasePort = String.Empty;  //数据库端口
        public DatabaseOperator() {
        }
        /// <summary>
        /// 数据库初始化构造函数
        /// </summary>
        /// <param name="DatabaseIP"></param>
        /// <param name="DatabaseUser"></param>
        /// <param name="DatabasePWD"></param>
        /// <param name="DatabaseName"></param>
        /// <param name="DatabasePort"></param>
        public DatabaseOperator(string DatabaseIP, string DatabaseUser, string DatabasePWD, string DatabaseName, string DatabasePort) {
            this.para_DatabaseIP = DatabaseIP;
            this.para_DatabaseUser = DatabaseUser;
            this.para_DatabasePWD = DatabasePWD;
            this.para_DatabaseName = DatabaseName;
            this.para_DatabasePort = DatabasePort;
            this.mysqlInfo = "server=" + this.para_DatabaseIP + ";User ID=" + this.para_DatabaseUser +
                 ";password=" + this.para_DatabasePWD + ";Database=" + this.para_DatabaseName + ";Port=" + DatabasePort + ";";
        }
        /// <summary>
        /// 从数据库查询数据
        /// </summary>
        /// <param name="queryString"></param>
        /// <param name="returnString"></param>
        /// <returns>是否执行成功</returns>
        public int queryMysqlDatabase(string queryString, ref ArrayList returnString) {
            returnString.Add(0);
            //String mysqlInfo = "server=192.168.191.3;User ID=keyboard;password=keyboard;Database=keyboardAuth";
            MySqlConnection connection = new MySqlConnection(mysqlInfo);
            MySqlDataAdapter queryAdapter = new MySqlDataAdapter();
            MySqlCommand queryExecution = new MySqlCommand(queryString, connection);
            queryExecution.CommandTimeout = 10;
            queryAdapter.SelectCommand = queryExecution;
            DataSet queryResult = new DataSet();
            //Console.WriteLine(connection.State);
            connection.Open();
            //Console.WriteLine(connection.State);
            if (!(connection.State == ConnectionState.Open)) { return 0; }
            queryAdapter.SelectCommand.ExecuteNonQuery();
            connection.Close();
            queryAdapter.Fill(queryResult);
            //遍历所有的Row
            foreach (DataRow dr in queryResult.Tables[0].Rows) {
                //遍历所有的列
                foreach (DataColumn dc in queryResult.Tables[0].Columns)
                {
                    //表名,列名,单元格数据
                    //Console.WriteLine("{0},{1}", dc.ColumnName, dr[dc]);
                    if (dc.ToString() == "user_name") {
                        returnString.Add(dr[dc].ToString());
                    }
                    else if (dc.ToString() == "key_serial") {
                        returnString.Add(dr[dc].ToString());
                    }
                    else if (dc.ToString() == "key_code") {
                        returnString.Add(dr[dc].ToString());
                    }
                    else if (dc.ToString() == "used_counts") {
                        returnString.Add(dr[dc].ToString());
                    }
                    else if (dc.ToString() == "update_time") {
                        returnString.Add(dr[dc]);
                    }
                    else { }
                }
                returnString[0] = 1;
            }
            //connection.ClearPoolAsync(connection);
            if (returnString.Count >= 2) { return 1; }
            return 0;
        }
        /// <summary>
        /// 更新数据库条目
        /// </summary>
        /// <param name="updateString"></param>
        /// <returns></returns>
        public int updateMysqlDatabase(string updateString) {
            MySqlConnection connection = new MySqlConnection(mysqlInfo);
            MySqlDataAdapter updateAdapter = new MySqlDataAdapter();
            MySqlCommand updateExecution = new MySqlCommand(updateString, connection);
            updateAdapter.UpdateCommand = updateExecution;
            connection.Open();
            if (!(connection.State == ConnectionState.Open)) { return 0; }
            int updateResult = updateAdapter.UpdateCommand.ExecuteNonQuery();
            connection.Close();
            //connection.ClearPoolAsync(connection);
            if (updateResult > 0) {
                return 1;
            }
            return 0;
        }
        /// <summary>
        /// 记录键盘开始使用的日志
        /// </summary>
        /// <param name="insertString"></param>
        /// <returns></returns>
        public int insertUseLogToMysqlDatabase(string insertString)
        {
            MySqlConnection connection = new MySqlConnection(mysqlInfo);
            MySqlDataAdapter insertAdapter = new MySqlDataAdapter();
            MySqlCommand insertExecution = new MySqlCommand(insertString, connection);
            insertAdapter.InsertCommand = insertExecution;
            connection.Open();
            if (!(connection.State == ConnectionState.Open)) { return 0; }
            int insertResult = insertAdapter.InsertCommand.ExecuteNonQuery();
            connection.Close();
            //connection.ClearPoolAsync(connection);
            if (insertResult > 0) {
                return 1;
            }
            return 0;
        }
        /// <summary>
        /// 记录键盘停止使用的日志（将停止使用时间补全）
        /// </summary>
        /// <param name="updateString"></param>
        /// <returns></returns>
        public int updateStopUseLogToMysqlDatabase(string updateString) {
            MySqlConnection connection = new MySqlConnection(mysqlInfo);
            MySqlDataAdapter updateAdapter = new MySqlDataAdapter();
            MySqlCommand updateExecution = new MySqlCommand(updateString, connection);
            updateAdapter.UpdateCommand = updateExecution;
            connection.Open(); 
            if (!(connection.State == ConnectionState.Open)) { return 0; }
            int updateResult = updateAdapter.UpdateCommand.ExecuteNonQuery();
            connection.Close();
            //connection.ClearPoolAsync(connection);
            if (updateResult > 0) {
                return 1;
            }
            return 0;
        }
    }
}
