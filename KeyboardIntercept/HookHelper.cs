﻿using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Windows;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading.Tasks;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
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
            //
            if (para_currentInputAllow == 0 && (wParam == WM_RBUTTONDOWN || wParam == WM_RBUTTONUP || wParam == WM_RBUTTONDBLCLK))
            {
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
            //键盘放行后每次单击调用截图方法截图
            //para_currentInputAllow == 0 && || wParam == WM_LBUTTONUP
            if (para_currentInputAllow == 1 && (wParam == WM_LBUTTONDOWN || wParam == WM_LBUTTONDBLCLK))
            {
                //Console.WriteLine("按了左键");
                string para_localScreenCaptureLocation = @"D:\\ScreenCapture";
                if (!Directory.Exists(para_localScreenCaptureLocation))
                    Directory.CreateDirectory(para_localScreenCaptureLocation);

                String thisTimeFileName = System.DateTime.Now.ToString("yyyy.MM.dd_hh.mm.ss");
                CaptureDesktop(para_localScreenCaptureLocation + "\\" + thisTimeFileName + ".jpg");
            }
            return CallNextHookEx(_hMouseHook, nCode, wParam, lParam);
        }
        //The ToAscii function translates the specified virtual-key code and keyboard state to the corresponding character or characters. The function translates the code using the input language and physical keyboard layout identified by the keyboard layout handle.
        //Import ScreenCapture DLL
        [System.Runtime.InteropServices.DllImport("gdi32.dll")]
        public static extern IntPtr CreateDC(string driver, string device, IntPtr res1, IntPtr res2);
        public enum TernaryRasterOperations
        {
            SRCCOPY = 0x00CC0020, /* dest = source*/
            SRCPAINT = 0x00EE0086, /* dest = source OR dest*/
            SRCAND = 0x008800C6, /* dest = source AND dest*/
            SRCINVERT = 0x00660046, /* dest = source XOR dest*/
            SRCERASE = 0x00440328, /* dest = source AND (NOT dest )*/
            NOTSRCCOPY = 0x00330008, /* dest = (NOT source)*/
            NOTSRCERASE = 0x001100A6, /* dest = (NOT src) AND (NOT dest) */
            MERGECOPY = 0x00C000CA, /* dest = (source AND pattern)*/
            MERGEPAINT = 0x00BB0226, /* dest = (NOT source) OR dest*/
            PATCOPY = 0x00F00021, /* dest = pattern*/
            PATPAINT = 0x00FB0A09, /* dest = DPSnoo*/
            PATINVERT = 0x005A0049, /* dest = pattern XOR dest*/
            DSTINVERT = 0x00550009, /* dest = (NOT dest)*/
            BLACKNESS = 0x00000042, /* dest = BLACK*/
            WHITENESS = 0x00FF0062, /* dest = WHITE*/
        };
        [DllImport("gdi32.dll")]
        public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest, int nWidth,
            int nHeight, IntPtr hObjSource, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        public static void CaptureDesktop(string sPath)
        {
            Rectangle rect = new Rectangle();
            rect.Width = Screen.PrimaryScreen.Bounds.Width;
            rect.Height = Screen.PrimaryScreen.Bounds.Height;
            IntPtr dcTmp = CreateDC("DISPLAY", "DISPLAY", (IntPtr)null, (IntPtr)null);
            Graphics gScreen = Graphics.FromHdc(dcTmp);
            Bitmap image = new Bitmap((int)(rect.Width), (int)(rect.Height), System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            Graphics gImage = Graphics.FromImage(image);
            IntPtr dcImage = gImage.GetHdc();
            IntPtr dcScreen = gScreen.GetHdc();
            BitBlt(dcImage, 0, 0, (int)(rect.Width), (int)(rect.Height), dcScreen, (int)(rect.Left), (int)(rect.Top), TernaryRasterOperations.SRCCOPY);
            gScreen.ReleaseHdc(dcScreen);
            gImage.ReleaseHdc(dcImage);
            //将格式转换为JPEG来保存
            ImageCodecInfo[] vImageCodecInfos = ImageCodecInfo.GetImageEncoders();
            foreach (ImageCodecInfo vImageCodecInfo in vImageCodecInfos)
            {
                if (vImageCodecInfo.FormatDescription.ToLower() == "jpeg")
                {
                    EncoderParameters vEncoderParameters = new EncoderParameters(1);
                    vEncoderParameters.Param[0] = new EncoderParameter(
                        System.Drawing.Imaging.Encoder.Quality, 75L);
                    image.Save(sPath, vImageCodecInfo, vEncoderParameters);
                    break;
                }
            }
            //以上将格式转换为JPEG保存
            //image.Save(sPath);
        }

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
        public int para_currentInputAllow = 0; //当前网络状态
        //////////////////////////////
        ////////参数定义区域///////////
        //////////////////////////////
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
            if (para_currentInputAllow == 0)
            {
                SendKeys.Send("");
                return 1;
            }
            if (nCode >= 0 && (wParam == WM_KEYDOWN || wParam == WM_SYSKEYDOWN || wParam == WM_KEYUP || wParam == WM_SYSKEYUP))
            {
                ///////////////////////////////////////////////////////////////////////////////////////////////
                KeyboardHookStruct MyKeyboardHookStruct = (KeyboardHookStruct)Marshal.PtrToStructure(lParam, typeof(KeyboardHookStruct));
                Keys keyData = (Keys)MyKeyboardHookStruct.vkCode;
                return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
            }
            SendKeys.Send("");
            return 1;
            //return CallNextHookEx(_hKeyboardHook, nCode, wParam, lParam);
        } 
    }
}
