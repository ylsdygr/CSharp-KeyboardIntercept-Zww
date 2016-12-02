using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyboardIntercept {
    class disableKeyboard : Form
    {
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
            return true;
        }
        
        public void rrun() {
            TextBox tbx = new TextBox();
            tbx.Parent = this;
        }
    }
}
