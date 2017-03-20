using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Collections;
using System.Net;
//以下两条引用在嵌入dll时使用
using System.Reflection;
using System.Resources;

namespace KeyboardIntercept {
    public partial class UserInterface : Form {
        /// <summary>
        /// 将DLL嵌入进exe的方法
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string dllName = args.Name.Contains(",") ? args.Name.Substring(0, args.Name.IndexOf(',')) : args.Name.Replace(".dll", "");
            dllName = dllName.Replace(".", "_");
            if (dllName.EndsWith("_resources")) return null;
            ResourceManager rm = new ResourceManager(GetType().Namespace + ".Properties.Resources", Assembly.GetExecutingAssembly());
            byte[] bytes = (byte[])rm.GetObject(dllName);
            return Assembly.Load(bytes);
        }
        public UserInterface() {
            //嵌入Dll的方法调用
            AppDomain.CurrentDomain.AssemblyResolve += new ResolveEventHandler(CurrentDomain_AssemblyResolve);
            //btnInstallHook_Click(null, null);
            InitializeComponent();
        }
        /// 声明一个hook对象
        GlobalHook hook;
        //初始化参数定义类对象
        ParametersDefine PD = new ParametersDefine();
        //设置一个定时器，每隔15秒执行一次
        System.Timers.Timer scanSavedImage = new System.Timers.Timer(15000); //设置时间间隔为15秒
        //U盘监控进程并自动识别
        public const int WM_DEVICECHANGE = 0x219;
        public const int DBT_DEVICEARRIVAL = 0x8000;
        public const int DBT_CONFIGCHANGECANCELED = 0x0019;
        public const int DBT_CONFIGCHANGED = 0x0018;
        public const int DBT_CUSTOMEVENT = 0x8006;
        public const int DBT_DEVICEQUERYREMOVE = 0x8001;
        public const int DBT_DEVICEQUERYREMOVEFAILED = 0x8002;
        public const int DBT_DEVICEREMOVECOMPLETE = 0x8004;
        public const int DBT_DEVICEREMOVEPENDING = 0x8003;
        public const int DBT_DEVICETYPESPECIFIC = 0x8005;
        public const int DBT_DEVNODES_CHANGED = 0x0007;
        public const int DBT_QUERYCHANGECONFIG = 0x0017;
        public const int DBT_USERDEFINED = 0xFFFF;

        protected override void WndProc(ref Message m)
        {
            try {
                if (m.Msg == WM_DEVICECHANGE) {
                    switch (m.WParam.ToInt32()) {
                        case WM_DEVICECHANGE:
                            break;
                        case DBT_DEVICEARRIVAL://U盘插入
                            DriveInfo[] s = DriveInfo.GetDrives();
                            foreach (DriveInfo drive in s)
                            {
                                if (drive.DriveType == DriveType.Removable)
                                {
                                    if (PD.para_UPanCounts >= 1) { break; }
                                    FunctionsIndex FI = new FunctionsIndex();
                                    FI.judgeUPanHasKeyFileOrNot(drive.Name.ToString(),ref PD.para_UPanFilePath,ref PD.para_currentUPanHasKeyFile);
                                    //System.Console.WriteLine(drive.Name.ToString());
                                    if (PD.para_currentUPanHasKeyFile == 0) {
                                        FI.clearStoredData(ref PD.para_currentAuthorizedKeys,ref PD.para_queryResult, ref PD.para_UPanFilePath,
                                        ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5,ref PD.para_theRightULetter,PD.para_localAuthFilePath);
                                        break; 
                                    }//U盘不包含授权文件，直接忽略
                                    //U盘有授权文件，判断网络
                                    PD.para_currentNetwork = 0;
                                    if (PD.para_useFileOrDatabase == 0) {
                                        PD.para_currentNetwork = FI.networkStatusJudge(PD.para_DatabaseIP);
                                    }
                                    else { PD.para_currentNetwork = FI.networkStatusJudge(PD.para_sharedIP); }
                                    if (PD.para_currentNetwork == 0 && PD.para_currentUPanHasKeyFile == 1){
                                        //网络断开且U盘中包含授权文件，则直接停止拦截
                                        PD.para_UPanCounts = 1;
                                        hook.para_currentInputAllow = 1;
                                        PD.para_currentInputAllow = 1;
                                        //hook.Stop();
                                        break;
                                    }
                                    else{//网络正常且U盘中包含授权文件的处理
                                        ProcessManager PM = new ProcessManager();
                                        //定义识别结果代码，2为识别失败，判断为Attacker，1为识别成功，更新及记录成功，0为识别成功，但更新及记录失败
                                        int recogResult = 2;
                                        if (PD.para_useFileOrDatabase == 0) {
                                            recogResult = PM.databaseRecognizeProcess(PD.para_DatabaseIP, PD.para_DatabaseUser,
                                                PD.para_DatabasePWD, PD.para_DatabaseName, PD.para_DatabasePort,
                                                ref PD.para_queryResult, PD.para_UPanFilePath, ref PD.para_currentInputAllow,
                                                ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5 );
                                        }
                                        else if (PD.para_useFileOrDatabase == 1) {
                                            recogResult = PM.fileRecognizeProcess(PD.para_netLoginCommand, PD.para_netLogFilePath,
                                                PD.para_localLogFilePath, PD.para_netAuthFilePath, PD.para_localAuthFilePath,
                                                ref PD.para_currentAuthorizedKeys, PD.para_UPanFilePath, ref PD.para_currentInputAllow,
                                                ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5);
                                        }
                                        else {
                                            recogResult = PM.databaseRecognizeProcess(PD.para_DatabaseIP, PD.para_DatabaseUser,
                                                PD.para_DatabasePWD, PD.para_DatabaseName, PD.para_DatabasePort,
                                                ref PD.para_queryResult, PD.para_UPanFilePath, ref PD.para_currentInputAllow,
                                                ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5);
                                            recogResult = PM.fileRecognizeProcess(PD.para_netLoginCommand, PD.para_netLogFilePath,
                                                PD.para_localLogFilePath, PD.para_netAuthFilePath, PD.para_localAuthFilePath,
                                                ref PD.para_currentAuthorizedKeys, PD.para_UPanFilePath, ref PD.para_currentInputAllow,
                                                ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5);
                                        }
                                        if (recogResult == 2) { PD.para_queryResult.Clear(); break; }
                                        if (recogResult == 0) { 
                                            //PD.para_UPanCounts = 1; 
                                            //hook.Stop();
                                            hook.para_currentInputAllow = 1;
                                            PD.para_UPanCounts = 1;
                                            PD.para_theRightULetter = drive.Name.ToString();
                                            break; }
                                        hook.para_currentInputAllow = 1;
                                        PD.para_UPanCounts = 1;
                                        PD.para_theRightULetter = drive.Name.ToString();
                                        //hook.Stop();
                                        break;
                                        //System.Console.WriteLine("UPanHasPlugin" + drive.Name.ToString());
                                    }
                                    //System.Console.WriteLine("UPanHasPlugin" + drive.Name.ToString());
                                    //lbKeyState.Text = "U盘已插入，盘符为:" + drive.Name.ToString();
                                    break;
                                }
                            }
                            break;
                        case DBT_CONFIGCHANGECANCELED:
                            break;
                        case DBT_CONFIGCHANGED:
                            break;
                        case DBT_CUSTOMEVENT:
                            break;
                        case DBT_DEVICEQUERYREMOVE:
                            break;
                        case DBT_DEVICEQUERYREMOVEFAILED:
                            break;
                        case DBT_DEVICEREMOVECOMPLETE:
                            {   //U盘卸载
                                if (Directory.Exists(PD.para_theRightULetter)) { break; }
                                if (PD.para_UPanCounts == 0) { break; }
                                if (PD.para_UPanCounts == 1) {
                                    FunctionsIndex FI = new FunctionsIndex();
                                    if (PD.para_currentNetwork == 1) {
                                        ProcessManager PM = new ProcessManager();
                                        if (PD.para_useFileOrDatabase == 0) {
                                            if (FI.networkStatusJudge(PD.para_DatabaseIP) == 1)
                                                PM.rejectUPanDatabaseProcess(PD.para_currentUKeyShow, PD.para_DatabaseIP, PD.para_DatabaseUser,
                                                PD.para_DatabasePWD,PD.para_DatabaseName,PD.para_DatabasePort);
                                        }
                                        else if (PD.para_useFileOrDatabase == 1) {
                                            PM.rejectUPanFileProcess(PD.para_netLoginCommand, PD.para_netLogFilePath, PD.para_localLogFilePath,
                                                PD.para_currentUKeyShow);
                                        }
                                        else {
                                            PM.rejectUPanDatabaseProcess(PD.para_currentUKeyShow, PD.para_DatabaseIP, PD.para_DatabaseUser,
                                                PD.para_DatabasePWD, PD.para_DatabaseName, PD.para_DatabasePort);
                                            PM.rejectUPanFileProcess(PD.para_netLoginCommand, PD.para_netLogFilePath, PD.para_localLogFilePath,
                                                PD.para_currentUKeyShow);
                                        }
                                    }
                                    PD.para_UPanCounts = 0;
                                    PD.para_currentUPanHasKeyFile = 0;
                                    PD.para_currentInputAllow = 0;
                                    hook.para_currentInputAllow = 0;
                                    FI.clearStoredData(ref PD.para_currentAuthorizedKeys, ref PD.para_queryResult, ref PD.para_UPanFilePath,
                                        ref PD.para_currentUKeyShow, ref PD.para_currentUKeyMD5,ref PD.para_theRightULetter, PD.para_localAuthFilePath);
                                    //hook.Start();
                                }
                                //System.Console.WriteLine("UPanhasUnpluged");
                            }
                            break;
                        case DBT_DEVICEREMOVEPENDING:
                            break;
                        case DBT_DEVICETYPESPECIFIC:
                            break;
                        case DBT_DEVNODES_CHANGED:
                            break;
                        case DBT_QUERYCHANGECONFIG:
                            break;
                        case DBT_USERDEFINED:
                            break;
                        default:
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                //System.Console.WriteLine(ex.Message.ToString());
                //MessageBox.Show(ex.Message);
            }
            base.WndProc(ref   m);
        }

        private void Form1_Load(object sender, EventArgs e) {
            btnInstallHook.Enabled = true;
            btnUnInstall.Enabled = false;
            //设定定时任务属性并且状态为启动
            scanSavedImage.Elapsed += new System.Timers.ElapsedEventHandler(Timer_TimesUp);
            scanSavedImage.AutoReset = true; //每到指定时间Elapsed事件是触发一次（false），还是一直触发（true）
            scanSavedImage.Enabled = true;
            scanSavedImage.Start();
            //初始化钩子对象
            if (hook == null)
            {
                hook = new GlobalHook();
                hook.KeyDown += new KeyEventHandler(hook_KeyDown);
                hook.KeyPress += new KeyPressEventHandler(hook_KeyPress);
                hook.KeyUp += new KeyEventHandler(hook_KeyUp);
                hook.OnMouseActivity += new MouseEventHandler(hook_OnMouseActivity);
                hook.Start();
            }

        }
        private void Form1_FormClosing(object sender, FormClosingEventArgs e) {
            if (btnUnInstall.Enabled == true) {
                hook.Stop();
            }
        }
        private void Timer_TimesUp(object sender, System.Timers.ElapsedEventArgs e)
        {
            //到达指定时间触发扫描一次指定目录
            FunctionsIndex getIP = new FunctionsIndex();
            string currentMachineIP = getIP.getCurrentHostIP();
            /*IPAddress[] localIPs;
                localIPs = Dns.GetHostAddresses(Dns.GetHostName());
                StringCollection IpCollection = new StringCollection();
                foreach (IPAddress ip in localIPs) {
                    //根据AddressFamily判断是否为ipv4,如果是InterNetWorkV6则为ipv6
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        IpCollection.Add(ip.ToString());
                }
                string[] IpArray = new string[IpCollection.Count];
                IpCollection.CopyTo(IpArray, 0);
                string thisHostIP =  IpArray[0];
            */
            //定义文件服务器的地址信息
            string para_fileUploadServerAddress = @"http://192.168.30.126:9010/Uploaded_Files/";
            string para_localScreenCaptureImageDIR = @"D:\\ScreenCapture";
            //System.Diagnostics.Debug.WriteLine(currentMachineIP);
            if (!Directory.Exists(para_localScreenCaptureImageDIR)) { return; }
            DirectoryInfo sourceDIR = new DirectoryInfo(para_localScreenCaptureImageDIR);
            if ((sourceDIR.GetFiles().Length + sourceDIR.GetDirectories().Length) == 0) { return; }
            WebClient myWebClient = new WebClient();
            FileInfo[] filesInDIR = sourceDIR.GetFiles();
            foreach (FileInfo currentFile in filesInDIR) {
                //Console.WriteLine(currentFile.FullName);
                try {
                    myWebClient.UploadFile(para_fileUploadServerAddress + currentMachineIP + "/", "POST", currentFile.FullName);
                    File.Delete(currentFile.FullName);
                }
                catch (Exception ex) { }
            }
        }
        private void btnInstallHook_Click(object sender, EventArgs e) {
            if (btnInstallHook.Enabled == true) {
                bool r = hook.Start();
                if (r) {
                    btnInstallHook.Enabled = false;
                    btnUnInstall.Enabled = true;
                //    MessageBox.Show("安装钩子成功!");
                }
                else {
                    MessageBox.Show("安装钩子失败!");
                }
            }
        }
        private void btnUnInstall_Click(object sender, EventArgs e) {
            if (btnUnInstall.Enabled == true) {
                hook.Stop();
                btnUnInstall.Enabled = false;
                btnInstallHook.Enabled = true;
            //    MessageBox.Show("卸载钩子成功!");
            }
        }
        /// 鼠标移动事件
        void hook_OnMouseActivity(object sender, MouseEventArgs e) {
            lbMouseState.Text = "X:" + e.X + " Y:" + e.Y;
        }
        /// 键盘抬起
        void hook_KeyUp(object sender, KeyEventArgs e) {
            //e.KeyData= 

            //lbKeyState.Text = "键盘抬起, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }
        /// 键盘输入
        void hook_KeyPress(object sender, KeyPressEventArgs e) {
           //MessageBox.Show("按下了");
        }
        /// 键盘按下
        void hook_KeyDown(object sender, KeyEventArgs e) {
            //针对消息处理的回显
            lbKeyState.Text = "开始识别密码";
            lbKeyState.Text = "键盘按下, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }
        /// <summary>
        /// 继续父类WinForm对窗口是否显示的方法
        /// </summary>
        protected override CreateParams CreateParams {
             get
             {
                 const int WS_EX_APPWINDOW = 0x40000;
                 const int WS_EX_TOOLWINDOW = 0x80;
                 CreateParams cp = base.CreateParams;
                 cp.ExStyle &= (~WS_EX_APPWINDOW);    // 不显示在TaskBar  
                 cp.ExStyle |= WS_EX_TOOLWINDOW;      // 不显示在Alt-Tab  
                 return cp;
             }
        }

    }
}
