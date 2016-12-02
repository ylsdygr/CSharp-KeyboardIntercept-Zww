using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
//using System.Threading;

namespace KeyboardIntercept {
    public partial class UserInterface : Form {
        public UserInterface() {
            InitializeComponent();
            //btnInstallHook_Click(null, null);
        }
        /// 声明一个hook对象
        GlobalHook hook;
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
