using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;
using System.Threading;

namespace KeyboardIntercept
{
    class ProcessManager
    {
        public string newKeySerial = "NULL";
        public string newKeyCode = "NULL";
        /// <summary>
        /// U盘中存在授权文件且网络正常时执行文件模式的识别，授权序列、授权码更新，状态设置过程
        /// </summary>
        /// <param name="para_netLoginCommand"></param>
        /// <param name="para_netLogFilePath"></param>
        /// <param name="para_localLogFilePath"></param>
        /// <param name="para_netAuthFilePath"></param>
        /// <param name="para_localAuthFilePath"></param>
        /// <param name="para_currentAuthorizedKeys"></param>
        /// <param name="para_UPanFilePath"></param>
        /// <param name="para_currentInputAllow"></param>
        /// <param name="para_currentUKeyShow"></param>
        /// <param name="para_currentUKeyMD5"></param>
        /// <returns></returns>
        public int fileRecognizeProcess(string para_netLoginCommand, string para_netLogFilePath, string para_localLogFilePath,
            string para_netAuthFilePath, string para_localAuthFilePath, ref ArrayList para_currentAuthorizedKeys,
            string para_UPanFilePath,ref int para_currentInputAllow,ref string para_currentUKeyShow,ref string para_currentUKeyMD5)
        {
            FilesOperator PMFO = new FilesOperator();
            FunctionsIndex PMFI = new FunctionsIndex();
            PMFO.filesCopy(para_netLoginCommand, para_netLogFilePath, para_localLogFilePath);
            int copyResult = PMFO.filesCopy(para_netLoginCommand, para_netAuthFilePath, para_localAuthFilePath);
            if (copyResult == 0) { return 0; }
            copyResult = PMFO.readAndOutputKeysInArrayList(para_localAuthFilePath, ref para_currentAuthorizedKeys);
            if (copyResult == 0) { return 0; }
            string uCode = PMFO.readKeyFromU(para_UPanFilePath);
            para_currentInputAllow = PMFI.compareAuthorizedKeysUtoNet(uCode, para_currentAuthorizedKeys,
                ref para_currentUKeyShow,ref para_currentUKeyMD5);
            if (para_currentInputAllow == 0) {
                this.useRecognizeFailedIntoLog(para_localLogFilePath);
                PMFO.filesCopy(para_netLoginCommand, para_localLogFilePath, para_netLogFilePath);
                PMFO.fileDelete(para_localLogFilePath);
                return 2; 
            }
            this.updateFileProcess(para_currentUKeyShow, ref para_currentAuthorizedKeys,
                para_localAuthFilePath, para_UPanFilePath, para_currentUKeyMD5);
            //更新开始使用的日志
            this.updateUseLogProcess(para_localLogFilePath, para_currentUKeyShow, 0);
            //将授权文件复制回服务器上
            PMFO.filesCopy(para_netLoginCommand, para_localLogFilePath, para_netLogFilePath);
            copyResult = PMFO.filesCopy(para_netLoginCommand, para_localAuthFilePath, para_netAuthFilePath);
            if (copyResult == 0) { PMFO.writeCodeToU(para_UPanFilePath, para_currentUKeyMD5); }
            //删除本地使用过的文件
            PMFO.fileDelete(para_localAuthFilePath);
            PMFO.fileDelete(para_localLogFilePath);
            return 1;
        }
        /// <summary>
        /// 文件授权模式下授权通过后更新本地授权序列，U盘授权文件;
        /// </summary>
        /// <param name="oldKeySerial"></param>
        /// <param name="currentKeySerials"></param>
        /// <param name="localAuthFilePath"></param>
        /// <param name="uPanFilePath"></param>
        /// <param name="uPanOldCode"></param>
        /// <returns></returns>
        public int updateFileProcess(string oldKeySerial, ref ArrayList currentKeySerials,
            string localAuthFilePath,string uPanFilePath,string uPanOldCode)
        {
            FunctionsIndex PMFI = new FunctionsIndex();
            int usedCounts = PMFI.calcUsedCounts(oldKeySerial);
            string AuthorizedUserName = PMFI.calcAuthorizedUser(oldKeySerial);
            if (this.newKeySerial == "NULL") {
                this.getNewKeySerialCode(AuthorizedUserName, usedCounts);
            }
            FilesOperator PMFO = new FilesOperator();
            int writeResult = PMFO.writeCodeToU(uPanFilePath,this.newKeyCode);
            if (writeResult == 0) { return 0; }
            PMFI.replaceKeySerialInArrayList(oldKeySerial, this.newKeySerial, ref currentKeySerials);
            writeResult = PMFO.writeKeyListsToAuthorizedFile(localAuthFilePath, currentKeySerials);
            if (writeResult == 0) { PMFO.writeCodeToU(uPanFilePath, uPanOldCode); return 0; }
            return 1;
        }
        /// <summary>
        /// 生成新的授权码
        /// </summary>
        /// <param name="AuthorizedUserName"></param>
        /// <param name="usedCounts"></param>
        public void getNewKeySerialCode(string AuthorizedUserName, int usedCounts) {
            FunctionsIndex PMFI = new FunctionsIndex();
            this.newKeySerial = PMFI.authorizedKeysGenerator(AuthorizedUserName, usedCounts);
            this.newKeyCode = PMFI.calcMD5(this.newKeySerial);
        }
        /// <summary>
        /// 文件模式下写入本地授权使用键盘开始与停止日志
        /// </summary>
        /// <param name="localLogFilePath"></param>
        /// <param name="oldKeySerial"></param>
        /// <param name="startOrStop"></param>
        /// <returns></returns>
        public int updateUseLogProcess(string localLogFilePath, string oldKeySerial,int startOrStop) {
            FunctionsIndex PMFI = new FunctionsIndex();
            string UseLog = "";
            if (startOrStop == 0) { UseLog = PMFI.useStartLogGenerator(oldKeySerial); }
            else { UseLog = PMFI.useStopLogGenerator(oldKeySerial); }
            FilesOperator PMFO = new FilesOperator();
            int writeResult = PMFO.writeLogToFile(localLogFilePath, UseLog);
            if (writeResult == 0) { return 0; }
            return 1;
        }
        /// <summary>
        /// 文件模式下更新本地授权失败时的日志
        /// </summary>
        /// <param name="localLogFilePath"></param>
        /// <returns></returns>
        public int useRecognizeFailedIntoLog(string localLogFilePath) {
            FunctionsIndex PMFI = new FunctionsIndex();
            string startUseLog = PMFI.useRecognizeFailedIntoLog();
            FilesOperator PMFO = new FilesOperator();
            int writeResult = PMFO.writeLogToFile(localLogFilePath, startUseLog);
            if (writeResult == 0) { return 0; }
            return 1;
        }
        /// <summary>
        /// 文件模式下拨出U盘更新日志过程
        /// </summary>
        /// <param name="para_netLoginCommand"></param>
        /// <param name="para_netLogFilePath"></param>
        /// <param name="para_localLogFilePath"></param>
        /// <param name="para_currentUKeyShow"></param>
        /// <returns></returns>
        public int rejectUPanFileProcess(string para_netLoginCommand, string para_netLogFilePath, string para_localLogFilePath,
            string para_currentUKeyShow)
        {
            FilesOperator PMFO = new FilesOperator();
            int result = PMFO.filesCopy(para_netLoginCommand, para_netLogFilePath, para_localLogFilePath);
            if (result == 0) { return 0; }
            result = this.updateUseLogProcess(para_localLogFilePath, para_currentUKeyShow, 1);
            if (result == 0) { return 0; }
            result = PMFO.filesCopy(para_netLoginCommand, para_localLogFilePath, para_netLogFilePath);
            if (result == 0) { return 0; }
            PMFO.fileDelete(para_localLogFilePath);
            return 1;
        }

        ///////////////数据库模式//////////////////
        /// <summary>
        /// 数据库模式下的授权码查询及更新
        /// </summary>
        /// <param name="para_DatabaseIP"></param>
        /// <param name="para_DatabaseUser"></param>
        /// <param name="para_DatabasePWD"></param>
        /// <param name="para_DatabaseName"></param>
        /// <param name="para_DatabasePort"></param>
        /// <param name="para_queryResult"></param>
        /// <param name="para_UPanFilePath"></param>
        /// <param name="para_currentInputAllow"></param>
        /// <param name="para_currentUKeyShow"></param>
        /// <param name="para_currentUKeyMD5"></param>
        /// <returns></returns>
        public int databaseRecognizeProcess(string para_DatabaseIP, string para_DatabaseUser, string para_DatabasePWD,
            string para_DatabaseName, string para_DatabasePort, ref ArrayList para_queryResult, string para_UPanFilePath,
            ref int para_currentInputAllow, ref string para_currentUKeyShow, ref string para_currentUKeyMD5)
        {
            FunctionsIndex PMFI = new FunctionsIndex();
            FilesOperator PMFO = new FilesOperator();
            DatabaseOperator PMDO = new DatabaseOperator(para_DatabaseIP, para_DatabaseUser, para_DatabasePWD, para_DatabaseName, para_DatabasePort);
            string uCode = PMFO.readKeyFromU(para_UPanFilePath);
            string queryString = "select number,user_name,key_serial,key_code,used_counts,update_time from authorized_lists where key_code = '" + uCode + "'";
            para_currentInputAllow = PMDO.queryMysqlDatabase(queryString, ref para_queryResult);
            if (para_currentInputAllow == 0)
            {
                string ipAddress = PMFI.getCurrentHostIP();
                this.useRecognizeFailedIntoDatabase(para_DatabaseIP, para_DatabaseUser, para_DatabasePWD, para_DatabaseName,
                    para_DatabasePort, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), ipAddress, System.Environment.MachineName, System.Environment.UserName);
                return 2;
            }
            para_currentUKeyShow = para_queryResult[2].ToString();
            para_currentUKeyMD5 = para_queryResult[3].ToString();

            int usedCounts = PMFI.calcUsedCounts(para_currentUKeyShow);
            string AuthorizedUserName = PMFI.calcAuthorizedUser(para_currentUKeyShow);
            if (this.newKeySerial == "NULL")
            {
                this.getNewKeySerialCode(AuthorizedUserName, usedCounts);
            }
            int writeResult = PMFO.writeCodeToU(para_UPanFilePath, this.newKeyCode);
            if (writeResult == 0) { return 0; }
            string updateString = @"update authorized_lists set key_serial =" + "'" + this.newKeySerial + "'," +
                @"key_code ='" + this.newKeyCode +
                @"',used_counts = '" + usedCounts +
                @"',update_time ='" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") +
                @"' where user_name = '" + para_queryResult[1].ToString() + "'";
            try{
                writeResult = PMDO.updateMysqlDatabase(updateString);
            }catch(Exception ex){ }
            if (writeResult == 0) { PMFO.writeCodeToU(para_UPanFilePath, para_currentUKeyMD5); return 0; }

            string currentipAddress = PMFI.getCurrentHostIP();
            string insertString = @"insert into used_log(user,start_use_time,ip_address,computer_name,computer_username,used_counts) values ('" +
                AuthorizedUserName + "','" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "','" + currentipAddress + "','" + System.Environment.MachineName + "','" +
                System.Environment.UserName + "'," + usedCounts + ")";
            try{
                writeResult = PMDO.insertUseLogToMysqlDatabase(insertString);
            }catch(Exception ex){ }
            if (writeResult == 0) { return 0; }
            return 1;
        }
        /// <summary>
        /// 数据库模式下更新本地授权失败时的日志
        /// </summary>
        /// <param name="para_DatabaseIP"></param>
        /// <param name="para_DatabaseUser"></param>
        /// <param name="para_DatabasePWD"></param>
        /// <param name="para_DatabaseName"></param>
        /// <param name="para_DatabasePort"></param>
        /// <param name="startUseTime"></param>
        /// <param name="ipAddress"></param>
        /// <param name="computerName"></param>
        /// <param name="computerUsername"></param>
        /// <param name="user"></param>
        /// <param name="usedCounts"></param>
        /// <returns></returns>
        public int useRecognizeFailedIntoDatabase(string para_DatabaseIP, string para_DatabaseUser, string para_DatabasePWD,
            string para_DatabaseName, string para_DatabasePort, string startUseTime, string ipAddress, string computerName,
            string computerUsername, string user = "Attacker", string usedCounts = "0")
        {
            string insertString = @"insert into used_log(user,start_use_time,ip_address,computer_name,computer_username,used_counts) values ('" +
                user + "','" + startUseTime + "','" + ipAddress + "','" + computerName + "','" + computerUsername + "'," + usedCounts + ")";
            DatabaseOperator PMDO = new DatabaseOperator(para_DatabaseIP, para_DatabaseUser, para_DatabasePWD, para_DatabaseName, para_DatabasePort);
            int insertResult = 0;
            try { insertResult = PMDO.insertUseLogToMysqlDatabase(insertString); }
            catch (Exception ex) { }
            if (insertResult == 0) { return 0; }
            return 1;
        }
        /// <summary>
        /// 数据库模式下弹出已授权U盘时的停止使用日志时间点更新
        /// </summary>
        /// <param name="currentUKeyShow"></param>
        /// <param name="para_DatabaseIP"></param>
        /// <param name="para_DatabaseUser"></param>
        /// <param name="para_DatabasePWD"></param>
        /// <param name="para_DatabaseName"></param>
        /// <param name="para_DatabasePort"></param>
        /// <returns></returns>
        public int rejectUPanDatabaseProcess(string currentUKeyShow, string para_DatabaseIP,
            string para_DatabaseUser, string para_DatabasePWD, string para_DatabaseName, string para_DatabasePort)
        {
            FunctionsIndex PMFI = new FunctionsIndex();
            int usedCounts = PMFI.calcUsedCounts(currentUKeyShow);
            string AuthorizedUserName = PMFI.calcAuthorizedUser(currentUKeyShow);
            string updateString = @"update used_log set end_use_time = '" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "' where user = '" +
                AuthorizedUserName + "' and used_counts = " + usedCounts.ToString();
            DatabaseOperator PMDO = new DatabaseOperator(para_DatabaseIP, para_DatabaseUser, para_DatabasePWD, para_DatabaseName, para_DatabasePort);
            int updateResult = 0;
            try { updateResult = PMDO.updateMysqlDatabase(updateString); }
            catch (Exception ex) { }
            if (updateResult == 0) { return 0; }
            return 1;
        }
    }
}
