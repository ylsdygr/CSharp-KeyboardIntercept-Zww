using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Specialized;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Net.NetworkInformation;
using System.IO;
using System.Collections;
using System.Runtime.InteropServices;

namespace KeyboardIntercept
{
    public class FunctionsIndex
    {
        /// <summary>
        /// 当U盘拔出恢复各变量的初始值,将程序重置
        /// </summary>
        /// <param name="currentAuthorizedKeys"></param>
        /// <param name="para_queryResult"></param>
        /// <param name="para_UPanFilePath"></param>
        /// <param name="para_currentUKeyShow"></param>
        /// <param name="para_currentUKeyMD5"></param>
        /// <param name="para_theRightULetter"></param>
        /// <param name="para_localAuthFilePath"></param>
        public void clearStoredData(ref ArrayList currentAuthorizedKeys, ref ArrayList para_queryResult, ref string para_UPanFilePath,
            ref string para_currentUKeyShow,ref string para_currentUKeyMD5, ref string para_theRightULetter,string para_localAuthFilePath)
        {
            currentAuthorizedKeys.Clear();
            para_queryResult.Clear();
            para_UPanFilePath = "NULL";
            para_currentUKeyShow ="";
            para_currentUKeyMD5 = "";
            para_theRightULetter = "NULL";
            if (File.Exists(para_localAuthFilePath)) {File.Delete(para_localAuthFilePath); }
        }
        public FunctionsIndex() {
        }
        ~FunctionsIndex() { }
        /// <summary>
        /// 授权开始使用记录产生器(For File)
        /// </summary>
        /// <param name="currentUKeyShow"></param>
        /// <returns></returns>
        public string useStartLogGenerator(string currentUKeyShow)
        {
            string thisLog = "用户：";
            string nameCharacters = currentUKeyShow.Substring(18, 2);
            int nameCharactersCount = Convert.ToInt32(nameCharacters, 16);
            thisLog += currentUKeyShow.Substring(20, nameCharactersCount) + " ";
            thisLog += "开始时间：";
            thisLog = thisLog + System.DateTime.Now.Date.Year.ToString() + "." +
                System.DateTime.Now.Date.Month.ToString() + "." + System.DateTime.Now.Date.Day.ToString() + "-";
            thisLog = thisLog + System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." +
                System.DateTime.Now.Second.ToString();
            string currentHostName = System.Environment.MachineName;
            thisLog += " IP: " + this.getCurrentHostIP() + " ";
            thisLog += " 计算机名: " + currentHostName + " ";
            string currentUserName = System.Environment.UserName;
            thisLog += "计算机用户: " + currentUserName + " ";
            string stringcount = currentUKeyShow.Substring(82, 2);
            int keysUsedCount = Convert.ToInt32(stringcount, 16);
            keysUsedCount += 1;
            thisLog += " 该用户第 ";
            thisLog += keysUsedCount.ToString() + " 次使用本授权";
            return thisLog;
        }
        /// <summary>
        /// 授权访问失败记录产生器（for File）
        /// </summary>
        /// <returns></returns>
        public string useRecognizeFailedIntoLog()
        {
            string thisLog = "";
            thisLog += "有人试图使用计算机但授权失败,时间: ";
            thisLog = thisLog + System.DateTime.Now.Date.Year.ToString() + "." +
                System.DateTime.Now.Date.Month.ToString() + "." + System.DateTime.Now.Date.Day.ToString() + "-";
            thisLog = thisLog + System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." +
                System.DateTime.Now.Second.ToString();
            string currentHostName = System.Environment.MachineName;
            thisLog += " IP: " + this.getCurrentHostIP() + " ";
            thisLog += " 计算机名: " + currentHostName + " ";
            string currentUserName = System.Environment.UserName;
            thisLog += "计算机用户: " + currentUserName + " ";
            return thisLog;
        }
        /// <summary>
        /// 授权停止使用记录产生器（for File）
        /// </summary>
        /// <param name="currentUKeyShow"></param>
        /// <returns></returns>
        public string useStopLogGenerator(string currentUKeyShow)
        {
            string thisLog = "用户：";
            string nameCharacters = currentUKeyShow.Substring(18, 2);
            int nameCharactersCount = Convert.ToInt32(nameCharacters, 16);
            thisLog += currentUKeyShow.Substring(20, nameCharactersCount) + " ";
            thisLog += "停止时间：";
            thisLog = thisLog + System.DateTime.Now.Date.Year.ToString() + "." +
                System.DateTime.Now.Date.Month.ToString() + "." + System.DateTime.Now.Date.Day.ToString() + "-";
            thisLog = thisLog + System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." +
                System.DateTime.Now.Second.ToString();
            string currentHostName = System.Environment.MachineName;
            thisLog += " IP: " + this.getCurrentHostIP() + " ";
            thisLog += " 计算机名: " + currentHostName + " ";
            string currentUserName = System.Environment.UserName;
            thisLog += "计算机用户: " + currentUserName + " ";
            string stringcount = currentUKeyShow.Substring(82, 2);
            int keysUsedCount = Convert.ToInt32(stringcount, 16);
            keysUsedCount += 1;
            thisLog += " 该用户第 ";
            thisLog += keysUsedCount.ToString() + " 次使用本授权";
            return thisLog;
        }
        /// <summary>
        /// 获取当前计算机的IP地址组
        /// </summary>
        /// <returns></returns>
        public string getCurrentHostIP()
        {
            IPAddress[] localIPs;
            localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            StringCollection IpCollection = new StringCollection();
            foreach (IPAddress ip in localIPs) {
                //根据AddressFamily判断是否为ipv4,如果是InterNetWorkV6则为ipv6  
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    IpCollection.Add(ip.ToString());
            }
            string[] IpArray = new string[IpCollection.Count];
            IpCollection.CopyTo(IpArray, 0);
            return IpArray[0];
        }
        /// <summary>
        /// 返回新生成授权码中的人名
        /// </summary>
        /// <param name="oldKeySerial"></param>
        /// <returns></returns>
        public string calcAuthorizedUser(string oldKeySerial) {
            //准备新生成授权码中的人名字母数
            string nameCharacters = oldKeySerial.Substring(18, 2);
            int nameCharactersCount = Convert.ToInt32(nameCharacters, 16);
            //准备新生成授权码中的人名信息
            string authorizedUserFromU = oldKeySerial.Substring(20, nameCharactersCount);
            return authorizedUserFromU;
        }
        /// <summary>
        /// 返回新生成授权码中的次数
        /// </summary>
        /// <param name="oldKeySerial"></param>
        /// <returns></returns>
        public int calcUsedCounts(string oldKeySerial) {
            string stringcount = oldKeySerial.Substring(82, 8);
            int keysUsedCount = Convert.ToInt32(stringcount, 16);
            if (keysUsedCount == 2147483646) {
                keysUsedCount = 0;
            }
            else {
                keysUsedCount += 1;
            }
            return keysUsedCount;
        }
        /// <summary>
        /// 替换原授权列表中的旧序列
        /// </summary>
        /// <param name="oldKeySerial"></param>
        /// <param name="newKeySrial"></param>
        /// <param name="currentKeySerials"></param>
        public void replaceKeySerialInArrayList(string oldKeySerial,string newKeySrial,ref ArrayList currentKeySerials) {
            currentKeySerials.Add(newKeySrial);
            for (int i = 0; i < currentKeySerials.Count; i++) {
                if (string.Equals(currentKeySerials[i].ToString(), oldKeySerial, StringComparison.CurrentCulture)) {
                    currentKeySerials.RemoveAt(i);
                }
            }
        }
        /// <summary>
        /// 计算给定字符串的MD5值
        /// </summary>
        /// <returns></returns>
        public string calcMD5(string goingCalculatedKey)
        {
            MD5 thisMD5 = MD5.Create();
            byte[] thisByte = thisMD5.ComputeHash(Encoding.UTF8.GetBytes(goingCalculatedKey));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < thisByte.Length; i++) {
                sBuilder.Append(thisByte[i].ToString("x2"));
            }
            string calced = sBuilder.ToString();
            calced = calced.ToUpper();
            return calced;
        }
        /// <summary>
        /// 校验码计算器
        /// </summary>
        /// <param name="calcedString"></param>
        /// <returns></returns>
        private char calculateCheckCode(string calcedString)
        {
            char checkCode = '@';
            int calcCheckSum = 0;
            foreach (char c in calcedString) {
                calcCheckSum += (int)c;
            }
            checkCode = (char)((calcCheckSum % 26) + 65);
            return checkCode;
        }
        /// <summary>
        /// 随机字符串生成器
        /// </summary>
        /// <param name="randomBits"></param>
        /// <returns></returns>
        private string randomValuesGenerator(int randomBits)
        {
            int number;
            string randomValues = String.Empty;
            System.Random random = new Random();
            for (int i = 0; i < randomBits; i++) {
                number = random.Next();
                number = number % 62;
                if (number < 10) {
                    number += 48;
                }
                else if (number > 9 && number < 36) {
                    number += 55;
                }
                else {
                    number += 61;
                }
                randomValues += ((char)number).ToString();
            }
            return randomValues;
        }
        /// <summary>
        /// 授权记录生成器
        /// </summary>
        /// <param name="nameNumber"></param>
        /// <param name="ConstantInformations"></param>
        /// <param name="calcCount"></param>
        /// <returns></returns>
        public string authorizedKeysGenerator(string authorizedUser, int calcCount)
        {
            int nameNumber = authorizedUser.Length;
            string authorizedKey = String.Empty;
            authorizedKey = randomValuesGenerator(18);
            string nameCharacters = nameNumber.ToString("x2");
            authorizedKey += nameCharacters;
            authorizedKey += authorizedUser;
            int afterNameRandomChars = 30 - nameNumber;
            authorizedKey += randomValuesGenerator(afterNameRandomChars);
            string yearFoutBits = System.DateTime.Now.Date.Year.ToString();
            authorizedKey += yearFoutBits.Substring(2, 2);
            authorizedKey += randomValuesGenerator(8);
            int month_bit = System.DateTime.Now.Date.Month;
            string month_2bit = month_bit.ToString();
            if (month_bit < 10) { month_2bit = "0" + System.DateTime.Now.Date.Month.ToString(); }
            authorizedKey += month_2bit;
            authorizedKey += randomValuesGenerator(8);
            int day_bit = System.DateTime.Now.Date.Day;
            string day_2bit = day_bit.ToString();
            if (day_bit < 10) { day_2bit = "0" + System.DateTime.Now.Date.Day.ToString(); }
            authorizedKey += day_2bit;
            authorizedKey += randomValuesGenerator(10);
            //authorizedKey += "0x";
            string countString = calcCount.ToString("x2");
            int zeroCount = 8 - countString.Length;
            string complementZero = "";
            for (int i = 0; i < zeroCount; i++) {
                complementZero += "0";
            }
            countString = complementZero + countString;
            authorizedKey += countString;
            authorizedKey += randomValuesGenerator(8);
            authorizedKey += this.calculateCheckCode(authorizedKey).ToString();
            return authorizedKey;
        }
        /// <summary>
        ///                                                                         c
        /// </summary>
        /// <param name="passwdKeyInUPan"></param>
        /// <param name="para_currentAuthorizedKeys"></param>
        /// <param name="currentUKeyShow"></param>
        /// <param name="currentUKeyMD5"></param>
        /// <returns></returns>
        public int compareAuthorizedKeysUtoNet(string passwdKeyInUPan, ArrayList para_currentAuthorizedKeys,
            ref string currentUKeyShow, ref string currentUKeyMD5)
        {
            foreach (string item in para_currentAuthorizedKeys) {
                String md5result = this.calcMD5(item);
                //System.Console.WriteLine(md5result);
                //System.Console.WriteLine(passwdKey);
                if (string.Equals(md5result, passwdKeyInUPan, StringComparison.CurrentCulture)) {
                    currentUKeyShow = item;
                    currentUKeyMD5 = passwdKeyInUPan;
                    return 1;
                }
            }
            return 0;
        }
        /// <summary>
        /// 记录下当前授权U盘的盘符
        /// </summary>
        /// <param name="parain_UPanLetter"></param>
        /// <param name="para_UPanFilePath"></param>
        /// <param name="para_currentUPanHasKeyFile"></param>
        public void judgeUPanHasKeyFileOrNot(string parain_UPanLetter, ref string para_UPanFilePath, ref int para_currentUPanHasKeyFile)
        {
            string temp_UPanKeyFile = parain_UPanLetter;
            temp_UPanKeyFile += "PrioData.exe";
            if (File.Exists(temp_UPanKeyFile)) {
                para_UPanFilePath = temp_UPanKeyFile;
                para_currentUPanHasKeyFile = 1;
            }
            else { }
        }
        /// <summary>
        /// 判断网络是否可用
        /// </summary>
        /// <param name="Description"></param>
        /// <param name="ReservedValue"></param>
        /// <returns></returns>
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(int Description, int ReservedValue);
        public static bool IsConnectInternet()
        {
            int Description = 0;
            return InternetGetConnectedState(Description, 0);
        }
        /// <summary>
        /// 判断指定的IP地址是否可连通
        /// </summary>
        /// <param name="parain_IP"></param>
        /// <returns></returns>
        public int networkStatusJudge(string parain_IP) {
  //          if (IsConnectInternet()) {
            try
            {
                Ping ping = new Ping();
                PingReply pingReply = ping.Send(parain_IP);
                if (pingReply.Status == IPStatus.Success)
                {
                    //Console.WriteLine("OnLine,ping Success!");
                    return 1;
                }
                return 0;
            }
            catch (Exception ex) {
                return 0;
            }
 //           }
        } 
    }
}
