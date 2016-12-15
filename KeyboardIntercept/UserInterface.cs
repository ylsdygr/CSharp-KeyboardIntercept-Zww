using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
//using System.Threading;

namespace KeyboardIntercept {
    public partial class UserInterface : Form {
        public UserInterface() {
            InitializeComponent();
            //btnInstallHook_Click(null, null);
        }
        /// 声明一个hook对象
        GlobalHook hook;
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

        protected override void WndProc(ref   Message m)
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
                                    hook.calcCurrentParameters(ref hook.para_localLogFilePath, ref hook.para_netLogFilePath);
                                    if (hook.para_UPanCounts > 1) { break; }
                                    if (hook.para_currentNetwork == 0) { hook.networkStatusJudge(hook.para_sharedIP); }
                                    hook.judgeUPanHasKeyFileOrNot(drive.Name.ToString());
                                    //System.Console.WriteLine(drive.Name.ToString());
                                    if (hook.para_currentUPanHasKeyFile == 0) { break; }//U盘不包含授权文件，直接忽略
                                    else if (hook.para_currentNetwork == 0 && hook.para_currentUPanHasKeyFile == 1){
                                        //网络断开且U盘中包含授权文件，则直接停止拦截
                                        hook.para_UPanCounts = 1;
                                        hook.Stop();
                                        break;
                                    }
                                    else{//网络正常且U盘中包含授权文件的处理
                                        hook.authorizedKeyFilesLogFileCopy();
                                        hook.compareAuthorizedKeysUtoNet();
                                        if (hook.para_currentInputAllow == 1) {
                                            hook.para_UPanCounts = 1;
                                            hook.updateProcess(hook.para_localLogFilePath,hook.para_netLogFilePath);
                                            hook.Stop();
                                            break;
                                        }
                                        else {
                                            hook.failureProcess(hook.para_localLogFilePath, hook.para_netLogFilePath);
                                        }
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
                                if (hook.para_UPanCounts == 0) { break; }
                                if (hook.para_UPanCounts == 1) {
                                    if (hook.para_currentNetwork == 1) { hook.stopUseProcess(hook.para_localLogFilePath, hook.para_netLogFilePath); }
                                    hook.para_UPanCounts = 0;
                                    hook.para_currentUPanHasKeyFile = 0;
                                    hook.para_currentInputAllow = 0;
                                    hook.clearStoredData();
                                    hook.Start();
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
         void hook_KeyDown(object sender, KeyEventArgs e)
        {
            //针对消息处理的回显
            lbKeyState.Text = "开始识别密码";
            lbKeyState.Text = "键盘按下, " + e.KeyData.ToString() + " 键码:" + e.KeyValue;
        }

    }
}
