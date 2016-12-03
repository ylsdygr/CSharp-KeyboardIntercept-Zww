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
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
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
                        //case WM_RBUTTONUP: 
                        //case WM_RBUTTONDBLCLK: 
                        button = MouseButtons.Right;
                        break;
                }
                int clickCount = 0;
                if (button != MouseButtons.None)
                    if (wParam == WM_LBUTTONDBLCLK || wParam == WM_RBUTTONDBLCLK)
                        clickCount = 2;
                    else clickCount = 1;

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
        
        //以下两个定义的标记在U盘拨出时会清除。
        int cutOrNot = 0;//判断接入U盘后按的第一个触发授权检查的键是否生效
        int judgedOrNot = 0;//是否对U盘中的授权文件进行对比，0未对比，1对比过
        int compareSuccess = 0;//标记是否对比成功，0未成功，1成功过
        string paraConstantInformations = "ConstantInformations";//固定的单串信息，字符必须是20个
        string para_netFilePath = "Y:\\PrioList.list";//网络上的授权文件地址,映射盘符的
        //string para_netFilePath = "\\\\192.168.6.86\\PrioList.list";//网络上的授权文件地址
        string para_logFilePath = "Y:\\KeyboardUse.chLog";//U盘使用日志文件路径
        string para_uFilePath = "A:";//本地U盘授权文件地址，默认字符串仅判断是否被修改使用
        int currentUKeyCount = 0;//当前U盘中存储的访问次数
        string currentUKeyShow = "";//当前U盘存储的授权码明文
        string currentUKeyMD5 = "";//当前U盘存储的授权码密文

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
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                //以下判断远程授权文件不存在时操作
                if(!File.Exists(para_netFilePath)){
                    //Stop();//停止键盘控制，此后网络恢复后不再控制
                    return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);//远程授权文件恢复后，继续拦截键盘
                }
                if (judgedOrNot == 0 || compareSuccess == 0)//未对比或对比失败就开始对比
                {
                    judgedOrNot = 1;//标记已对比过
                    string hasUpan = this.isUpan();
                    if (string.Equals("A:", hasUpan, StringComparison.CurrentCulture))
                    {
                        SendKeys.Send("");
                        return 1;
                    }
                    else {
                        para_uFilePath = hasUpan;
                    }
                    para_uFilePath += "\\PrioData.list";
                    if (para_uFilePath == "A:\\PrioData.list")
                    {
                        SendKeys.Send("");
                        return 1;
                    }
                    //以下判断本地U盘中是否有授权文件  及 远程授权列表是否可访问
                    if (!File.Exists(para_uFilePath) || !File.Exists(para_netFilePath)) {
                        SendKeys.Send("");
                        return 1;
                    }
                    string[] passwdArray = this.readAndOutputKeysIn(para_netFilePath);
                    string passwdKey = this.readKeyFromU(para_uFilePath);
                    foreach (string item in passwdArray)
                    {
                        String md5result = this.calcMD5(item);
                        if (string.Equals(md5result, passwdKey, StringComparison.CurrentCulture))
                        {
                            currentUKeyShow = item;
                            currentUKeyMD5 = passwdKey;
                            cutOrNot = 1;
                            compareSuccess = 1;//对比成功了
                            this.updateKeyToUAndAuthorized(para_netFilePath, para_uFilePath);
                            this.useLogGenerator();
                        }
                    }
                    //以下的判断针对第一次对比授权的结果选择第一次的按键是否正常发送
                    if (compareSuccess == 1 && cutOrNot == 1){
                        return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
                    }
                    else {
                        SendKeys.Send("");
                        return 1;
                    }
                }
                else
                { //如果已对比过，则根据对比结果来判断是否要将以后的按键发送。
                    para_uFilePath = this.isUpan();
                    para_uFilePath += "\\PrioData.list";
                    if (para_uFilePath == "A:\\PrioData.list" || !File.Exists(para_uFilePath))
                    {
                        //U盘拨出，将三个标识重置
                        cutOrNot = 0;
                        judgedOrNot = 0;
                        compareSuccess = 0;
                        para_uFilePath = "A:";
                        currentUKeyCount = 0;
                        currentUKeyShow = "";
                        currentUKeyMD5 = "";
                        SendKeys.Send("");
                        return 1;
                    }
                    else{
                        return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
                    }
                }
            }
            return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        }
        /// <summary>
        /// 授权记录生成器
        /// </summary>
        /// <param name="ConstantInformations"></param>
        /// <param name="calcCount"></param>
        /// <returns></returns>
        private string authorizedKeysGenerator(string ConstantInformations, int calcCount)
        {
            string authorizedKey = String.Empty;
            authorizedKey = randomValuesGenerator(20);
            authorizedKey += ConstantInformations;
            authorizedKey += randomValuesGenerator(10);
            string yearFoutBits = System.DateTime.Now.Date.Year.ToString();
            authorizedKey += yearFoutBits.Substring(2,2);
            authorizedKey += randomValuesGenerator(8);
            authorizedKey += System.DateTime.Now.Date.Month.ToString();
            authorizedKey += randomValuesGenerator(8);
            authorizedKey += System.DateTime.Now.Date.Day.ToString();
            authorizedKey += randomValuesGenerator(8);
            authorizedKey += "0x";
            authorizedKey += calcCount.ToString("x2");
            authorizedKey += randomValuesGenerator(14);
            authorizedKey += this.calculateCheckCode(authorizedKey).ToString();
            
            return authorizedKey;
        }
        /// <summary>
        /// 随机字符串生成器
        /// </summary>
        /// <param name="randomBits"></param>
        /// <returns></returns>
        private string randomValuesGenerator(int randomBits){
            int number;
            string randomValues = String.Empty;
            System.Random random = new Random();
            for (int i = 0; i < randomBits; i++){
                number = random.Next();
                number = number % 62;
                if (number < 10){
                    number += 48;
                }
                else if (number > 9 && number < 36){
                    number += 55;
                }
                else{
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
        private char calculateCheckCode(string calcedString){
            char checkCode = '@';
            int calcCheckSum = 0;
            foreach(char c in calcedString){
                calcCheckSum += (int)c;
            }
            checkCode = (char)((calcCheckSum % 26)+65);
            return checkCode;
        }   
        /// <summary>
        /// 以每次读取一行的形式读取授权码,返回字符数组
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string[] readAndOutputKeysIn(string filePath)
        {
            ArrayList readedKeysList = new ArrayList();
            try
            {
                FileInfo setProtect = new FileInfo(filePath);
                setProtect.Attributes = FileAttributes.Normal;
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile)) {
                    while(!stream.EndOfStream){
                        readedKeysList.Add(stream.ReadLine());
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e){
            }
            string[] readedKeys = (string[])readedKeysList.ToArray(typeof(string));
            return readedKeys;
        }
        /// <summary>
        /// 以每次读取一行的形式读取授权码,返回ArrayList
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private ArrayList readAndOutputKeysInArrayList(string filePath)
        {
            ArrayList readedKeysList = new ArrayList();
            if (!File.Exists(filePath)){
                return readedKeysList;
            }
            try
            {
                FileInfo setProtect = new FileInfo(filePath);
                setProtect.Attributes = FileAttributes.Normal;
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile))
                {
                    while (!stream.EndOfStream)
                    {
                        readedKeysList.Add(stream.ReadLine());
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e){
            }
            return readedKeysList;
        }
        /// <summary>
        /// 读取授权U盘中的一行授权码
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private string readKeyFromU(string filePath)
        {
            String readedKeys = "";
            if(!File.Exists(filePath)){
                return readedKeys;
            }
            try
            {
                FileStream keysInFile = new FileStream(filePath, FileMode.Open);
                using (var stream = new StreamReader(keysInFile))
                {
                    while (!stream.EndOfStream)
                    {
                        readedKeys = stream.ReadLine();
                    }
                }
                keysInFile.Close();
            }
            catch (IOException e){
            }
            return readedKeys;
        }
        /// <summary>
        /// 产生新的授权码存储在U盘中与授权码文件中
        /// </summary>
        /// <param name="netFilePath"></param>
        /// <param name="uFilePath"></param>
        /// <returns></returns>
        private int updateKeyToUAndAuthorized(string netFilePath,string uFilePath) {
            //-------------------写入U盘-----------------//
            if (!File.Exists(netFilePath) || !File.Exists(uFilePath))
            {
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
            else {
                keysUsedCount += 1;
            }
            //准备新生成授权码中的固定信息
            string stringConstantInformationFromU = currentUKeyShow.Substring(20,20);
            //新生成一串授权码
            string newKey = this.authorizedKeysGenerator(stringConstantInformationFromU, keysUsedCount);
            try {
                FileStream outputToU = new FileStream(uFilePath, FileMode.Open);
                using (var stream = new StreamWriter(outputToU)){
                    stream.Write(this.calcMD5(newKey));
                }
                outputToU.Close();
                isUSuccessed = 1;
            }catch(IOException e){
                isUSuccessed = 0;
            }
            //-------------------写入U盘线束-----------------//
            //-------------------写入远程授权文件-----------------//
            if(isUSuccessed == 1){//U盘写入成功后写入授权文件
                ArrayList readedKeysList = new ArrayList();
                readedKeysList = this.readAndOutputKeysInArrayList(netFilePath);
                readedKeysList.Add(newKey);
                for (int i = 0; i < readedKeysList.Count; i++){
                    if (string.Equals(readedKeysList[i].ToString(), currentUKeyShow, StringComparison.CurrentCulture))
                    {
                        readedKeysList.RemoveAt(i);
                    }
                }
                try{
                    if(File.Exists(netFilePath)){
                        FileInfo setProtect = new FileInfo(para_netFilePath);
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

                        
                        setProtect.Attributes = FileAttributes.Hidden | FileAttributes.ReadOnly ;
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
            if (isNetSuccessed == 0) {
                try{
                    FileStream recoverU = new FileStream(uFilePath, FileMode.Open);
                    using (var stream = new StreamWriter(recoverU))
                    {
                        stream.Write(currentUKeyMD5);
                    }
                    recoverU.Close();
                }
                catch (IOException e){
                }
            }
            //-------------------重新写入U盘授权文件结束-----------------//
            return 0;
        }
        /// <summary>
        /// 判断是否有U盘,如果有，则选中第一个返回其盘符
        /// </summary>
        /// <returns></returns>
        private string isUpan(){
            string uPanList = "A:";

            DriveInfo[] allDevice = DriveInfo.GetDrives();
            foreach (DriveInfo drive in allDevice){
                if (drive.DriveType == DriveType.Removable)
                {
                    uPanList = drive.Name.ToString();
                }
            }
            return uPanList;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private string calcMD5(string goingCalculatedKey) {
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
        private void useLogGenerator() {
            string thisLog = "用户：";
            thisLog += currentUKeyShow.Substring(20, 20) + " ";
            thisLog += "开始使用时间：";
            thisLog = thisLog + System.DateTime.Now.Date.Year.ToString() + "." + 
                System.DateTime.Now.Date.Month.ToString() + "." + System.DateTime.Now.Date.Day.ToString() + " ";
            thisLog = thisLog + System.DateTime.Now.Hour.ToString() + "." + System.DateTime.Now.Minute.ToString() + "." + 
                System.DateTime.Now.Second.ToString();
            try
            {
                if (File.Exists(para_logFilePath)){
                    FileStream writeLog = new FileStream(para_logFilePath, FileMode.Append);
                    using (var stream = new StreamWriter(writeLog))
                    {
                        stream.Write(thisLog);
                        stream.Write("\n");
                    }
                    writeLog.Close();
                }
                else {
                    FileStream writeLog = new FileStream(para_logFilePath, FileMode.OpenOrCreate);
                    using (var stream = new StreamWriter(writeLog))
                    {
                        stream.Write(thisLog);
                        stream.Write("\n");
                    }
                    writeLog.Close();
                }
            }
            catch (IOException e){
            }
        }
        //从文件中将授权码读取到程序中，读取形式为每次100个字符
        /*
        private string[] readAndOutputKeysIn(string filePath){
            ArrayList readedKeysList = new ArrayList();
            string keyInOneLine = "";
            byte[] byData = new byte[100];
            char[] charData = new char[100];

            try {
                FileStream keysInFile = new FileStream(filePath,FileMode.Open);
                keysInFile.Seek(0, SeekOrigin.Begin);
                keysInFile.Read(byData, 0, 99);
                Decoder d = Encoding.Default.GetDecoder();
                d.GetChars(byData, 0, byData.Length, charData, 0);
                keyInOneLine = charData.ToString();
                keysInFile.Close();
            }catch(IOException e){
                //System.Console.WriteLine(e.ToString());
            }
            readedKeysList.Add(keyInOneLine);
            string[] readedKeys = (string[])readedKeysList.ToArray(typeof(string));
            return readedKeys;
        }*/
    }
}
