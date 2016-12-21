using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Collections;

namespace KeyboardIntercept
{
    public class FilesOperator
    {
        public FilesOperator() {
        }
        /// <summary>
        /// 将源文件路径复制到目录文件路径
        /// </summary>
        /// <param name="netLoginPath"></param>
        /// <param name="oriFilePath"></param>
        /// <param name="destFilePath"></param>
        /// <returns></returns>
        public int filesCopy(string netLoginPath, string oriFilePath, string destFilePath)
        {
            //System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.Process.Start("net.exe", netLoginPath);
            //p.StartInfo.FileName = "net.exe";
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardInput = true;//接受来自调用程序的输入信息
            //p.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
            //p.StartInfo.RedirectStandardError = true;//重定向标准错误输出
            //p.StartInfo.CreateNoWindow = true;//不显示程序窗口
            //p.Start();
            //p.StandardInput.WriteLine(para_netLoginPath + "&&exit");
            //p.StandardInput.AutoFlush = true;
            //System.Console.WriteLine(p.StandardOutput.ReadToEnd());
            if (File.Exists(oriFilePath)){
                try {
                    File.Copy(oriFilePath, destFilePath, true);
                }
                catch (IOException ex) { return 0; }
                return 1;
            }
            //System.Console.WriteLine("File is no exists , Please Contact Administrator");
            return 0;
        }
        /// <summary>
        /// 删除指定路径的文件
        /// </summary>
        /// <param name="delFilePath"></param>
        /// <returns></returns>
        public int fileDelete(string delFilePath) {
            if (File.Exists(delFilePath))
            {
                try {
                    File.Delete(delFilePath);
                }
                catch (IOException ex) { return 0; }
                return 1;
            }
            return 0;
        }
        /// <summary>
        /// 以每次读取一行的形式读取授权码,正确返回1，错误返回0;
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="currentAuthorizedKeys">以引用值读出授权列表</param>
        /// <returns></returns>
        public int readAndOutputKeysInArrayList(string filePath, ref ArrayList currentAuthorizedKeys)
        {
            ArrayList readedArrayList = new ArrayList();
            if (!File.Exists(filePath)) { 
                //System.Console.WriteLine("File is not exists!");
                return 0;
            }
            try {
                //FileInfo setProtect = new FileInfo(filePath);
                //setProtect.Attributes = FileAttributes.Normal;
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile)) {
                    while (!stream.EndOfStream) {
                        currentAuthorizedKeys.Add(stream.ReadLine());
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e) { return 0; }
            return 1;
        }
        /// <summary>
        /// 将日志记录写入至文件
        /// </summary>
        /// <param name="localLogFilePath"></param>
        /// <param name="goingToWriteString"></param>
        /// <returns></returns>
        public int writeLogToFile(string localLogFilePath, string goingToWriteString)
        {
            try {
                if (File.Exists(localLogFilePath))
                {
                    FileStream writeLog = new FileStream(localLogFilePath, FileMode.Append);
                    using (var stream = new StreamWriter(writeLog)) {
                        stream.Write(goingToWriteString);
                        stream.Write("\n");
                    }
                    writeLog.Close();
                }
                else {
                    FileStream writeLog = new FileStream(localLogFilePath, FileMode.OpenOrCreate);
                    using (var stream = new StreamWriter(writeLog))
                    {
                        stream.Write(goingToWriteString);
                        stream.Write("\n");
                    }
                    writeLog.Close();
                }
            }
            catch (IOException ex) { return 0; }
            return 1;
        }
        /// <summary>
        /// 将新的授权序列写入授权列表中
        /// </summary>
        /// <param name="localAuthFilePath"></param>
        /// <param name="currentKeysList"></param>
        /// <returns></returns>
        public int writeKeyListsToAuthorizedFile(string localAuthFilePath, ArrayList currentKeysList)
        {
            if (!File.Exists(localAuthFilePath)){
                return 0;
            }
            try {
                //FileInfo setProtect = new FileInfo(localAuthFilePath);
                //setProtect.Attributes = FileAttributes.Normal;
                FileStream outputToNet = new FileStream(localAuthFilePath, FileMode.Open);
                using (var stream = new StreamWriter(outputToNet)) {
                    foreach (string item in currentKeysList) {
                        stream.Write(item);
                        stream.Write("\n");
                    }
                }
                outputToNet.Close();
                //setProtect.Attributes = FileAttributes.Hidden;
            }
            catch (IOException ex) { 
                //Console.WriteLine(ex.ToString());
                return 0; 
            }
            return 1;
        }
        /// <summary>
        /// 将新的授权码写入U盘中
        /// </summary>
        /// <param name="uFilePath"></param>
        /// <param name="goingToWriteCode"></param>
        /// <returns></returns>
        public int writeCodeToU(string uFilePath, string goingToWriteCode)
        {
            if (!File.Exists(uFilePath)) {
                return 0;
            }
            try {
                FileStream outputToU = new FileStream(uFilePath, FileMode.Open);
                using (var stream = new StreamWriter(outputToU)) {
                    stream.Write(goingToWriteCode);
                }
                outputToU.Close();
            }
            catch (IOException e) {
                return 0;
            }
            return 1;
        }
        /// <summary>
        /// 读取授权U盘中的第一行授权码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public string readKeyFromU(string filePath)
        {
            String readedKeys = "";
            if (!File.Exists(filePath)) {
                return readedKeys;
            }
            try {
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile)) {
                    while (!stream.EndOfStream) {
                        readedKeys = stream.ReadLine();
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e) { }
            return readedKeys;
        }
    }
}
