using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections;

namespace KeyboardIntercept
{
    public class ParametersDefine
    {
        //////////////////////////////
        ////////参数定义区域///////////
        //////////////////////////////
        public int para_useFileOrDatabase = 0;  //此项配置系统使用数据库、文件，0数据库，1文件，2均使用(此项不建议使用，还有Bug)
        public int para_UPanCounts = 0;  //定义U盘总数，防止多个U盘同时使用
        public int para_currentNetwork = 0; //当前网络状态，0为无网络，1为有网络
        public string para_UPanFilePath = "NULL"; //记录含有授权U盘的授权文件路径
        public string para_theRightULetter = "NULL"; //记录正常授权的U盘盘符
        public int para_currentUPanHasKeyFile = 0;//记录当前U盘是否含有授权文件
        public ArrayList para_currentAuthorizedKeys = new ArrayList();//当前授权列表中所有的记录条目
        public int para_currentInputAllow = 0;//当前键盘是否允许输入，0是未允许，1是已允许
        public string para_currentUKeyShow = "";//当前U盘存储的授权码明文
        public string para_currentUKeyMD5 = "";//当前U盘存储的授权码密文
       
        //////////数据库配置//////////
        public string para_DatabaseIP = "10.10.10.10";  //数据库连接IP
        public string para_DatabaseUser = "keyboard";  //数据库连接用户名
        public string para_DatabasePWD = "keyboard";  //数据库连接密码
        public string para_DatabaseName = "keyboardAuth";  //数据库名称
        public string para_DatabasePort = "3306";  //数据库端口
        public ArrayList para_queryResult = new ArrayList(); //数据库查询到的当前授权序列条目
        //////////数据库配置//////////

        ////////共享授权文件//////////
        //string para_localAuthFilePath = "C:\\PrioList.exe";//网络上的授权文件地址,映射盘符的
        public string para_sharedIP = "10.10.10.10";//授权文件共享机器的IP地址，用于检测网络连通性
        public string para_netLoginCommand = " use \\\\10.10.10.10\\Shared  /user:\"User\" \"Passwd\"";
        public string para_netAuthFilePath = @"\\10.10.10.10\Shared\PrioList.exe";//网络上的授权文件地址
        public string para_localAuthFilePath = "C:\\Windows\\PrioList.exe";//网络上的授权文件复制到本地的地址
        public string para_netLogFilePath = @"\\10.10.10.10\Shared\KeyboardUseLog.exe";//授权使用日志文件路径
        public string para_localLogFilePath = "C:\\Windows\\KeyboardUseLog.exe";//授权使用日志文件路径        
        ////////共享授权文件//////////

        //////////////////////////////
        ////////参数定义区域///////////
        //////////////////////////////
        public ParametersDefine()
        { 
        }
    }
}
