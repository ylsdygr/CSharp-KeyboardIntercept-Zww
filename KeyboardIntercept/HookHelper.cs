using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using System.Security.AccessControl;
using System.Net.NetworkInformation;
using System.Net;
using System.Collections.Specialized;

namespace KeyboardIntercept
{
    /*
    注意：
        如果运行中出现SetWindowsHookEx的返回值为0，这是因为.net 调试模式的问题，具体的做法是禁用宿主进程，在 Visual Studio 中打开项目。
        在“项目”菜单上单击“属性”。
        单击“调试”选项卡。
        清除“启用 Visual Studio 宿主进程(启用windows承载进程)”复选框 或 勾选启用非托管代码调试
    */

    //Declare wrapper managed POINT class.
    [StructLayout(LayoutKind.Sequential)]
    public class POINT {
        public int x;
        public int y;
    }
    //Declare wrapper managed MouseHookStruct class.
    [StructLayout(LayoutKind.Sequential)]
    public class MouseHookStruct {
        public POINT pt;
        public int hwnd;
        public int wHitTestCode;
        public int dwExtraInfo;
    }        
    //Declare wrapper managed KeyboardHookStruct class.

    [StructLayout(LayoutKind.Sequential)]
    public class KeyboardHookStruct
    {
        public int vkCode; //Specifies a virtual-key code. The code must be a value in the range 1 to 254. 
        public int scanCode; // Specifies a hardware scan code for the key. 
        public int flags; // Specifies the extended-key flag, event-injected flag, context code, and transition-state flag.
        public int time; // Specifies the time stamp for this message.
        public int dwExtraInfo; // Specifies extra information associated with the message. 
    }
    

    public class GlobalHook
    {
        public delegate int HookProc(int nCode, Int32 wParam, IntPtr lParam);
        public delegate int GlobalHookProc(int nCode, Int32 wParam, IntPtr lParam);
        public GlobalHook()
        {
            //Start();
        }
        ~GlobalHook()
        {
            Stop();
        }
        public event MouseEventHandler OnMouseActivity;
        public event KeyEventHandler KeyDown;
        public event KeyPressEventHandler KeyPress;
        public event KeyEventHandler KeyUp;

        /// <summary>
        /// 定义鼠标钩子句柄.
        /// </summary>
        static int _hMouseHook = 0;
        /// <summary>
        /// 定义键盘钩子句柄
        /// </summary>
        static int _hKeyboardHook = 0;

        public int HMouseHook
        {
            get { return _hMouseHook; }
        }
        public int HKeyboardHook
        {
            get { return _hKeyboardHook; }
        }

        /// <summary>
        /// 鼠标钩子常量(from Microsoft SDK  Winuser.h )
        /// </summary>
        public const int WH_MOUSE_LL = 14;
        /// <summary>
        /// 键盘钩子常量(from Microsoft SDK  Winuser.h )
        /// </summary>
        public const int WH_KEYBOARD_LL = 13;

        /// <summary>
        /// 定义鼠标处理过程的委托对象
        /// </summary>
        GlobalHookProc MouseHookProcedure;
        /// <summary>
        /// 键盘处理过程的委托对象
        /// </summary>
        GlobalHookProc KeyboardHookProcedure;

        //导入window 钩子扩展方法导入

        /// <summary>
        /// 安装钩子方法
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int SetWindowsHookEx(int idHook, GlobalHookProc lpfn, IntPtr hInstance, int threadId);

        /// <summary>
        /// 卸载钩子方法
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern bool UnhookWindowsHookEx(int idHook);

        //Import for CallNextHookEx.
        /// <summary>
        /// 使用这个函数钩信息传递给链中的下一个钩子过程。
        /// </summary>
        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern int CallNextHookEx(int idHook, int nCode, Int32 wParam, IntPtr lParam);

        public bool Start()
        {
            // install Mouse hook 
            if (_hMouseHook == 0)
            {
                // Create an instance of HookProc.
                MouseHookProcedure = new GlobalHookProc(MouseHookProc);
                try
                {
                    _hMouseHook = SetWindowsHookEx(WH_MOUSE_LL,
                        MouseHookProcedure,
                        Marshal.GetHINSTANCE(
                        Assembly.GetExecutingAssembly().GetModules()[0]),
                        0);
                }
                catch (Exception err)
                { }
                //如果安装鼠标钩子失败
                if (_hMouseHook == 0)
                {
                    Stop();
                    return false;
                    //throw new Exception("SetWindowsHookEx failed.");
                }
            }
            //安装键盘钩子
            if (_hKeyboardHook == 0)
            {
                KeyboardHookProcedure = new GlobalHookProc(KeyboardHookProc);
                try
                {
                    _hKeyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL,
                        KeyboardHookProcedure,
                        Marshal.GetHINSTANCE(
                        Assembly.GetExecutingAssembly().GetModules()[0]),
                        0);
                }
                catch (Exception err2)
                { }
                //如果安装键盘钩子失败
                if (_hKeyboardHook == 0)
                {
                    Stop();
                    return false;
                    //throw new Exception("SetWindowsHookEx ist failed.");
                }
            }
            return true;
        }

        public void Stop()
        {
            bool retMouse = true;
            bool retKeyboard = true;
            if (_hMouseHook != 0)
            {
                retMouse = UnhookWindowsHookEx(_hMouseHook);
                _hMouseHook = 0;
            }
            if (_hKeyboardHook != 0)
            {
                retKeyboard = UnhookWindowsHookEx(_hKeyboardHook);
                _hKeyboardHook = 0;
            }
            //If UnhookWindowsHookEx fails.
            if (!(retMouse && retKeyboard))
            {
                //throw new Exception("UnhookWindowsHookEx ist failed.");
            }

        }
        /// <summary>
        /// 卸载hook,如果进程强制结束,记录上次钩子id,并把根据钩子id来卸载它
        /// </summary>
        public void Stop(int hMouseHook, int hKeyboardHook)
        {
            if (hMouseHook != 0)
            {
                UnhookWindowsHookEx(hMouseHook);
            }
            if (hKeyboardHook != 0)
            {
                UnhookWindowsHookEx(hKeyboardHook);
            }
        }

        private const int WM_MOUSEMOVE = 0x200;
        private const int WM_LBUTTONDOWN = 0x201;
        private const int WM_RBUTTONDOWN = 0x204;
        private const int WM_MBUTTONDOWN = 0x207;
        private const int WM_LBUTTONUP = 0x202;
        private const int WM_RBUTTONUP = 0x205;
        private const int WM_MBUTTONUP = 0x208;
        private const int WM_LBUTTONDBLCLK = 0x203;
        private const int WM_RBUTTONDBLCLK = 0x206;
        private const int WM_MBUTTONDBLCLK = 0x209;
        private int MouseHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {   
            //this.KeyboardHookProc(0,25,44456);
            //System.Console.WriteLine(WM_MOUSEMOVE);       //512
            //System.Console.WriteLine(WM_LBUTTONDOWN);     //513
            //System.Console.WriteLine(WM_LBUTTONUP);       //514
            //System.Console.WriteLine(WM_LBUTTONDBLCLK);   //515
            //System.Console.WriteLine(WM_RBUTTONDOWN);     //516
            //System.Console.WriteLine(WM_RBUTTONUP);       //517
            //System.Console.WriteLine(WM_RBUTTONDBLCLK);   //518
            //临时权限放行
            //keyboardCurrentAllow = 1;
            if (para_currentInputAllow == 0 &&
                (wParam == WM_RBUTTONDOWN || wParam == WM_RBUTTONUP || wParam == WM_RBUTTONDBLCLK)){
                return 1;
            }
            if ((nCode >= 0) && (OnMouseActivity != null))
            {
                MouseButtons button = MouseButtons.None;
                switch (wParam)
                {
                    case WM_LBUTTONDOWN:    //左键按下
                        //case WM_LBUTTONUP:    //右键按下
                        //case WM_LBUTTONDBLCLK:   //同时按下
                        button = MouseButtons.Left;
                        break;
                    case WM_RBUTTONDOWN: 
                        button = MouseButtons.Right;
                        break;
                    //case WM_RBUTTONUP:
                    //case WM_RBUTTONDBLCLK:
                }
                int clickCount = 0;
                if (button != MouseButtons.None)
                {
                    if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK){
                        clickCount = 2;
                    }
                    else{
                        clickCount = 1;
                    }
                }
                //Marshall the data from callback.
                MouseHookStruct MyMouseHookStruct =
                    (MouseHookStruct)Marshal.PtrToStructure(lParam, typeof(MouseHookStruct));
                MouseEventArgs e = new MouseEventArgs(
                    button,
                    clickCount,
                    MyMouseHookStruct.pt.x,
                    MyMouseHookStruct.pt.y,
                    0);
                OnMouseActivity(this, e);
            }
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }
        //The ToAscii function translates the specified virtual-key code and keyboard state to the corresponding character or characters. The function translates the code using the input language and physical keyboard layout identified by the keyboard layout handle.

        [DllImport("user32")]
        public static extern int ToAscii(int uVirtKey, //[in] Specifies the virtual-key code to be translated. 
            int uScanCode, // [in] Specifies the hardware scan code of the key to be translated. The high-order bit of this value is set if the key is up (not pressed). 
            byte[] lpbKeyState, // [in] Pointer to a 256-byte array that contains the current keyboard state. Each element (byte) in the array contains the state of one key. If the high-order bit of a byte is set, the key is down (pressed). The low bit, if set, indicates that the key is toggled on. In this function, only the toggle bit of the CAPS LOCK key is relevant. The toggle state of the NUM LOCK and SCROLL LOCK keys is ignored.
            byte[] lpwTransKey, // [out] Pointer to the buffer that receives the translated character or characters. 
            int fuState); // [in] Specifies whether a menu is active. This parameter must be 1 if a menu is active, or 0 otherwise. 
        //The GetKeyboardState function copies the status of the 256 virtual keys to the specified buffer. 
        [DllImport("user32")]
        public static extern int GetKeyboardState(byte[] pbKeyState);

        private const int WM_KEYDOWN = 0x100;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_SYSKEYUP = 0x105;
        //////////////////////////////
        ////////参数定义区域///////////
        //////////////////////////////
        //string para_localAuthFilePath = "C:\\PrioList.exe";//网络上的授权文件地址,映射盘符的
        string para_localAuthFilePath = @"D:\Windows\PrioList.exe";//网络上的授权文件复制到本地的地址
        public string para_localLogFilePath = @"D:\Windows\";//授权使用日志文件本地目录
        public string para_sharedIP = "10.16.139.81";//授权文件共享机器的IP地址，用于检测网络连通性
        string para_netLoginPath = " use \\\\10.16.139.81\\Shared  /user:\"User\" \"Passwd\"";
        string para_netAuthFilePath = @"\\10.16.139.81\Shared\PrioList.exe";//网络上的授权文件地址
        public string para_netLogFilePath = @"\\10.16.139.81\Shared\";//授权使用日志文件路径
        public int para_currentNetwork = 0; //当前网络状态，0为无网络，1为有网络
        public int para_UPanCounts = 0;  //定义U盘总数，防止多个U盘同时使用
        string para_UPanFilePath = "NULL"; //记录含有授权U盘的授权文件路径
        public int para_currentUPanHasKeyFile = 0;//记录当前U盘是否含有授权文件
        ArrayList para_currentAuthorizedKeys = new ArrayList();
        public int para_currentInputAllow = 0;//当前键盘是否允许输入，0是未允许，1是已允许
        string currentUKeyShow = "";//当前U盘存储的授权码明文
        string currentUKeyMD5 = "";//当前U盘存储的授权码密文
        //////////////////////////////
        ////////参数定义区域///////////
        //////////////////////////////
		//U盘接入时自动合成远程日志文件路径
        public void calcCurrentParameters(ref String localLogFilePath, ref String netLogFilePath) {
            localLogFilePath += "Log" + this.getCurrentHostIP() + ".exe";
            netLogFilePath += "Log" + this.getCurrentHostIP() + ".exe";
        }
        /// <summary>
        /// 判断当前共享授权文件的电脑是否在线
        /// </summary>
        /// <param name="parain_IP"></param>
        /// <returns></returns>
        public void networkStatusJudge(string parain_IP)
        {
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(parain_IP);
            if (pingReply.Status == IPStatus.Success) {
                //Console.WriteLine("OnLine,ping Success!");
                para_currentNetwork = 1;
            }
            else{
                //Console.WriteLine("Offline，ping Failed!");
                para_currentNetwork = 0;
            }
        }
        /// <summary>
        /// 记录下当前授权U盘的盘符
        /// </summary>
        /// <param name="parain_UPanLetter"></param>
        public void judgeUPanHasKeyFileOrNot(string parain_UPanLetter)
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
        /// 将远程授权文件及使用日志文件复制到本地
        /// </summary>
        public void authorizedKeyFilesLogFileCopy()
        {
            //System.Diagnostics.Process p = new System.Diagnostics.Process();
            System.Diagnostics.Process.Start("net.exe", para_netLoginPath);
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
            if (File.Exists(para_netAuthFilePath)) {
                File.Copy(para_netAuthFilePath, para_localAuthFilePath, true);
            }
            else {
                //System.Console.WriteLine("File is no exists , Please Contact Administrator");
            }
        }
        /// <summary>
        /// 以每次读取一行的形式读取授权码,返回ArrayList
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private void readAndOutputKeysInArrayList(string filePath)
        {
            if (!File.Exists(filePath)) { //System.Console.WriteLine("File is not exists!"); 
            }
            try {
                //FileInfo setProtect = new FileInfo(filePath);
                //setProtect.Attributes = FileAttributes.Normal;
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile)) {
                    while (!stream.EndOfStream) {
                        para_currentAuthorizedKeys.Add(stream.ReadLine());
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e) { }
        }
        /// <summary>
        /// 当U盘拔出恢复各变量的初始值,将程序重置为刚启动时。
        /// </summary>
        public void clearStoredData()
        {
            this.currentUKeyShow = "";
            this.currentUKeyMD5 = "";
            para_localLogFilePath = @"D:\Windows\";
            para_netLogFilePath = @"\\192.168.1.194\Shared\";
            para_UPanFilePath = "NULL";
            para_currentAuthorizedKeys.Clear();
        }
        /// <summary>
        /// 读取授权U盘中的一行授权码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string readKeyFromU(string filePath)
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
        /// <summary>
        /// 对比U盘中的授权码与授权文件中的条目
        /// </summary>
        public void compareAuthorizedKeysUtoNet()
        {
            this.readAndOutputKeysInArrayList(para_localAuthFilePath);
            //string[] passwdArray = this.readAndOutputKeysIn(para_localAuthFilePath);
            string passwdKey = this.readKeyFromU(para_UPanFilePath);
            foreach (string item in para_currentAuthorizedKeys) {
                String md5result = this.calcMD5(item);
                //System.Console.WriteLine(md5result);
                //System.Console.WriteLine(passwdKey);
                if (string.Equals(md5result, passwdKey, StringComparison.CurrentCulture)) {
                    currentUKeyShow = item;
                    currentUKeyMD5 = passwdKey;
                    para_currentInputAllow = 1;
                }
            }
        }
        /// <summary>
        /// 处理授权码及日志更新
        /// </summary>
        /// <param name="currentLocalUseLogFile"></param>
        /// <param name="currentNetUseLogFile"></param>
        public void updateProcess(String currentLocalUseLogFile, String currentNetUseLogFile)
        {
            try {
                if (File.Exists(currentNetUseLogFile)) {
                    File.Copy(currentNetUseLogFile, currentLocalUseLogFile, true);
                    this.useStartIntoLog(currentLocalUseLogFile);
                }
                else {
                    File.Create(currentLocalUseLogFile).Close();
                    this.useStartIntoLog(currentLocalUseLogFile);
                }
                File.Copy(currentLocalUseLogFile, currentNetUseLogFile, true);
                File.Delete(currentLocalUseLogFile);

            }
            catch (IOException ex) { Console.WriteLine(ex.ToString()); }
            
            this.updateKeyToUAndAuthorized(para_localAuthFilePath, para_UPanFilePath);
            File.Delete(para_localAuthFilePath);
        }
        /// <summary>
        /// 授权开始使用记录
        /// </summary>
        /// <param name="goingToWriteFilePath"></param>
        private void useStartIntoLog(string goingToWriteFilePath)
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
            thisLog += " 计算机名: "+ currentHostName + " ";
            string currentUserName = System.Environment.UserName;
            thisLog += "计算机用户: " + currentUserName + " ";
            string stringcount = currentUKeyShow.Substring(82, 2);
            int keysUsedCount = Convert.ToInt32(stringcount, 16);
            keysUsedCount += 1;
            thisLog += " 该用户第 ";
            thisLog += keysUsedCount.ToString() + " 次使用本授权";
            try {
                FileStream writeLog = new FileStream(goingToWriteFilePath, FileMode.Append);
                using (var stream = new StreamWriter(writeLog)) {
                    stream.Write(thisLog);
                    stream.Write("\n");
                }
                writeLog.Close();
            }
            catch (IOException ex) { Console.WriteLine(ex.ToString()); }
        }
        /// <summary>
        /// 认证失败的处理
        /// </summary>
        /// <param name="currentLocalUseLogFile"></param>
        /// <param name="currentNetUseLogFile"></param>
        public void failureProcess(String currentLocalUseLogFile, String currentNetUseLogFile) {
            try {
                if (File.Exists(currentNetUseLogFile)) {
                    File.Copy(currentNetUseLogFile, currentLocalUseLogFile, true);
                    this.useRecognizeFailedIntoLog(currentLocalUseLogFile);
                }
                else {
                    File.Create(currentLocalUseLogFile).Close();
                    this.useRecognizeFailedIntoLog(currentLocalUseLogFile);
                }
                File.Copy(currentLocalUseLogFile, currentNetUseLogFile, true);
                File.Delete(currentLocalUseLogFile);
            }
            catch (IOException ex) { Console.WriteLine(ex.ToString()); }
        }
        /// <summary>
        /// 如果用户试图在网络正常时访问授权但失败则记录日志
        /// </summary>
        /// <param name="goingToWriteFilePath">需要写入文件的文件路径</param>
        public void useRecognizeFailedIntoLog(String goingToWriteFilePath)
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
            try {
                FileStream writeLog = new FileStream(goingToWriteFilePath, FileMode.Append);
                using (var stream = new StreamWriter(writeLog)) {
                    stream.Write(thisLog);
                    stream.Write("\n");
                }
                writeLog.Close();
            }
            catch (IOException e) { }
        }
        /// <summary>
        /// 键盘停止使用时的日志记录过程
        /// </summary>
        /// <param name="currentLocalUseLogFile"></param>
        /// <param name="currentNetUseLogFile"></param>
        public void stopUseProcess(String currentLocalUseLogFile, String currentNetUseLogFile) {
            try {
                if (File.Exists(currentNetUseLogFile)) {
                    File.Copy(currentNetUseLogFile, currentLocalUseLogFile, true);
                    this.useStopIntoLog(currentLocalUseLogFile);
                }
                else {
                    File.Create(currentLocalUseLogFile).Close();
                    this.useStopIntoLog(currentLocalUseLogFile);
                }
                File.Copy(currentLocalUseLogFile, currentNetUseLogFile, true);
                File.Delete(currentLocalUseLogFile);
            }
            catch (IOException ex) { Console.WriteLine(ex.ToString()); }
        }
        /// <summary>
        /// 记录授权停止使用
        /// </summary>
        /// <param name="goingToWriteFilePath"></param>
        public void useStopIntoLog(String goingToWriteFilePath)
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
            try {
                FileStream writeLog = new FileStream(goingToWriteFilePath, FileMode.Append);
                using (var stream = new StreamWriter(writeLog)) {
                    stream.Write(thisLog);
                    stream.Write("\n");
                }
                writeLog.Close();
            }
            catch (IOException ex) { Console.WriteLine(ex.ToString()); }
        }
        /// <summary>
        /// 授权记录生成器
        /// </summary>
        /// <param name="ConstantInformations"></param>
        /// <param name="calcCount"></param>
        /// <returns></returns>
        private string authorizedKeysGenerator(int nameNumber, string ConstantInformations, int calcCount)
        {
            string authorizedKey = String.Empty;
            authorizedKey = randomValuesGenerator(18);
            string nameCharacters = nameNumber.ToString("x2");
            authorizedKey += nameCharacters;
            authorizedKey += ConstantInformations;
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
            authorizedKey += calcCount.ToString("x2");
            authorizedKey += randomValuesGenerator(14);
            authorizedKey += this.calculateCheckCode(authorizedKey).ToString();
            return authorizedKey;
        }
        /// <summary>
        /// 产生新的授权码存储在U盘中与授权码文件中
        /// </summary>
        /// <param name="netFilePath"></param>
        /// <param name="uFilePath"></param>
        /// <returns></returns>
        private int updateKeyToUAndAuthorized(string netFilePath, string uFilePath)
        {
            //-------------------写入U盘-----------------//
            if (!File.Exists(netFilePath) || !File.Exists(uFilePath)) {
                return 0;
            }
            int isUSuccessed = 0;//U盘是否写入成功，0失败，1成功
            int isNetSuccessed = 0;//授权文件是否写入成功，0失败，1成功

            //准备新生成授权码中的次数控制
            string stringcount = currentUKeyShow.Substring(82, 2);
            int keysUsedCount = Convert.ToInt32(stringcount, 16);
            if (keysUsedCount == 255)
            {
                keysUsedCount = 0;
            }
            else
            {
                keysUsedCount += 1;
            }
            //准备新生成授权码中的人名字母数
            string nameCharacters = currentUKeyShow.Substring(18, 2);
            int nameCharactersCount = Convert.ToInt32(nameCharacters, 16);
            //准备新生成授权码中的人名信息
            string stringConstantInformationFromU = currentUKeyShow.Substring(20, nameCharactersCount);
            //新生成一串授权码
            string newKey = this.authorizedKeysGenerator(nameCharactersCount, stringConstantInformationFromU, keysUsedCount);
            try
            {
                FileStream outputToU = new FileStream(uFilePath, FileMode.Open);
                using (var stream = new StreamWriter(outputToU))
                {
                    stream.Write(this.calcMD5(newKey));
                }
                outputToU.Close();
                isUSuccessed = 1;
            }
            catch (IOException e)
            {
                isUSuccessed = 0;
            }
            //-------------------写入U盘线束-----------------//
            //-------------------写入远程授权文件-----------------//
            if (isUSuccessed == 1)
            {//U盘写入成功后写入授权文件
                ArrayList readedKeysList = new ArrayList();
                readedKeysList = para_currentAuthorizedKeys;
                readedKeysList.Add(newKey);
                for (int i = 0; i < readedKeysList.Count; i++)
                {
                    if (string.Equals(readedKeysList[i].ToString(), currentUKeyShow, StringComparison.CurrentCulture))
                    {
                        readedKeysList.RemoveAt(i);
                    }
                }
                try
                {
                    if (File.Exists(netFilePath))
                    {
                        FileInfo setProtect = new FileInfo(para_localAuthFilePath);
                        setProtect.Attributes = FileAttributes.Normal;
                        FileStream outputToNet = new FileStream(netFilePath, FileMode.Open);
                        using (var stream = new StreamWriter(outputToNet))
                        {
                            foreach (string item in readedKeysList)
                            {
                                stream.Write(item);
                                stream.Write("\n");
                            }
                        }
                        outputToNet.Close();
                        isNetSuccessed = 1;
                        File.Copy(para_localAuthFilePath, para_netAuthFilePath, true);
                        setProtect.Attributes = FileAttributes.Hidden;
                        return 1;
                    }
                }
                catch (IOException e)
                {
                    isNetSuccessed = 0;
                }
            }
            //-------------------写入远程授权文件结束-----------------//
            //-------------------写入远程授权文件失败，将U盘文件恢复-------//
            if (isNetSuccessed == 0)
            {
                try
                {
                    FileStream recoverU = new FileStream(uFilePath, FileMode.Open);
                    using (var stream = new StreamWriter(recoverU))
                    {
                        stream.Write(currentUKeyMD5);
                    }
                    recoverU.Close();
                }
                catch (IOException e)
                {
                }
            }
            //-------------------重新写入U盘授权文件结束-----------------//
            return 0;
        }
        /// <summary>
        /// 计算给定字符串的MD5值
        /// </summary>
        /// <returns></returns>
        private string calcMD5(string goingCalculatedKey)
        {
            MD5 thisMD5 = MD5.Create();
            byte[] thisByte = thisMD5.ComputeHash(Encoding.UTF8.GetBytes(goingCalculatedKey));
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < thisByte.Length; i++)
            {
                sBuilder.Append(thisByte[i].ToString("x2"));
            }
            string calced = sBuilder.ToString();
            calced = calced.ToUpper();
            return calced;
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
            for (int i = 0; i < randomBits; i++)
            {
                number = random.Next();
                number = number % 62;
                if (number < 10)
                {
                    number += 48;
                }
                else if (number > 9 && number < 36)
                {
                    number += 55;
                }
                else
                {
                    number += 61;
                }
                randomValues += ((char)number).ToString();
            }

            return randomValues;
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
            foreach (char c in calcedString)
            {
                calcCheckSum += (int)c;
            }
            checkCode = (char)((calcCheckSum % 26) + 65);
            return checkCode;
        }
        /// <summary>
        /// 按键监测主函数
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns></returns>
        private int KeyboardHookProc(int nCode, Int32 wParam, IntPtr lParam)
        {
            //if (nCode >= 0 && compareSuccess == 1 && (wParam == WM_SYSKEYUP || wParam == WM_SYSKEYDOWN ))
            //{
             //   return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
            //}
            //if (nCode >= 0 && wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            if (nCode >= 0 && wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN)
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                SendKeys.Send("");
                return 1;
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                //return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
            }
            SendKeys.Send("");
            return 1;
            //return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }
		/// <summary>
        /// 获取当前计算机的IP地址组
        /// </summary>
        /// <returns>String</returns>
        public string getCurrentHostIP() {
            IPAddress[] localIPs;
            localIPs = Dns.GetHostAddresses(Dns.GetHostName());
            StringCollection IpCollection = new StringCollection();
            foreach (IPAddress ip in localIPs)
            {
                //根据AddressFamily判断是否为ipv4,如果是InterNetWorkV6则为ipv6  
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    IpCollection.Add(ip.ToString());
            }
            string[] IpArray = new string[IpCollection.Count];
            IpCollection.CopyTo(IpArray, 0);
            return IpArray[0];
        }
            
    }
}
