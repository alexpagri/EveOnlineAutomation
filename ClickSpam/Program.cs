﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.IO;
using System.Drawing;
using System.Timers;
using System.Threading;
using System.Windows;

namespace ClickSpam
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form3());
            //Application.Run(new Form2());
        }
    }
}
