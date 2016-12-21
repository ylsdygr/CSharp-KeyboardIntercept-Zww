using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.IO;

namespace KeyboardIntercept {
    public class Program  {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>  
        //[STAThread]  //单线程运行，文件模式下可正常使用
        [MTAThread]  //多线程运行，数据库模式下必须使用此选项
        //[ThreadStatic]
        static void Main() {
            //Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new UserInterface());
        }
    }
}