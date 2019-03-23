using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using MouseKeyboardActivityMonitor.WinApi;
using MouseKeyboardActivityMonitor;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;
using Tesseract;
using System.Drawing.Drawing2D;
using System.Text.RegularExpressions;

namespace ClickSpam
{
    public partial class Form3 : Form
    {

        [DllImport("user32")]
        public static extern int SetCursorPos(int x, int y);//Self Ex.

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(int dwFlags, int dx, int dy, int cButtons, int dwExtraInfo);//Click & Declick
        [DllImport("user32.dll")]
        static extern IntPtr GetDC(IntPtr hwnd);

        [DllImport("user32.dll")]
        static extern Int32 ReleaseDC(IntPtr hwnd, IntPtr hdc);

        [DllImport("gdi32.dll")]
        static extern uint GetPixel(IntPtr hdc, int nXPos, int nYPos);
        //EX:mouse_event(MOUSEEVENTF_LEFTDOWN,0,0,0,0);
        //EX:mouse_event(MOUSEEVENTF_LEFTUP,0,0,0,0);
        private const int MOUSEEVENTF_MOVE = 0x0001; /* mouse move */
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002; /* left button down */
        private const int MOUSEEVENTF_LEFTUP = 0x0004; /* left button up */
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008; /* right button down */
        private const int MOUSEEVENTF_RIGHTUP = 0x0010; /* right button up */
        private const int MOUSEEVENTF_MIDDLEDOWN = 0x0020; /* middle button down */
        private const int MOUSEEVENTF_MIDDLEUP = 0x0040; /* middle button up */
        private const int MOUSEEVENTF_XDOWN = 0x0080; /* x button down */
        private const int MOUSEEVENTF_XUP = 0x0100; /* x button down */
        private const int MOUSEEVENTF_WHEEL = 0x0800; /* wheel button rolled */
        private const int MOUSEEVENTF_VIRTUALDESK = 0x4000; /* map to entire virtual desktop */
        private const int MOUSEEVENTF_ABSOLUTE = 0x8000; /* absolute move */

        /*
         * BACKSPACE	{BACKSPACE}, {BS}, or {BKSP}
BREAK	{BREAK}
CAPS LOCK	{CAPSLOCK}
DEL or DELETE	{DELETE} or {DEL}
DOWN ARROW	{DOWN}
END	{END}
ENTER	{ENTER}or ~
ESC	{ESC}
HELP	{HELP}
HOME	{HOME}
INS or INSERT	{INSERT} or {INS}
LEFT ARROW	{LEFT}
NUM LOCK	{NUMLOCK}
PAGE DOWN	{PGDN}
PAGE UP	{PGUP}
PRINT SCREEN	{PRTSC}
RIGHT ARROW	{RIGHT}
SCROLL LOCK	{SCROLLLOCK}
TAB	{TAB}
UP ARROW	{UP}
F1	{F1}
F2	{F2}
F3	{F3}
F4	{F4}
F5	{F5}
F6	{F6}
F7	{F7}
F8	{F8}
F9	{F9}
F10	{F10}
F11	{F11}
F12	{F12}
F13	{F13}
F14	{F14}
F15	{F15}
F16	{F16}
	
To specify keys combined with any combination of the SHIFT, CTRL, and ALT keys, precede the key code with one or more of the following codes:

Key	Code
SHIFT	+
CTRL 	^
ALT	%
         */

        private readonly KeyboardHookListener m_KeyboardHookManager;//init KeyboardHook
        private readonly MouseHookListener m_MouseHookManager;//init KeyboardHook

        private int[,] module = new int[10, 4];
        private int[] item5 = new int[6], item7 = new int[8];
        private int mod3, mod9, maxlocktgt = 6;
        private bool zeroleft, continuegate;
        private string[] vect = new string[300];
        private string cobj;
        private Stopwatch watchs = new Stopwatch();
        StreamReader sre = new StreamReader("read.txt");
        TesseractEngine engine = new TesseractEngine(@"./tessdata", "eng", EngineMode.Default);
        private int mouse_c = 0;
        int cx, cy, in_on = 0;

        int counter = 0, currentBelt = 1, CyDu = 163, isFirst = 1, maxBelt = 9, crsa = 0, noaddcounter = 0, isRestarting = 0, avaM = 0, gmx = 0, gmy = 0;
        int[] ModulesV = new int[3];


        public Form3()
        {
            InitializeComponent();
            m_KeyboardHookManager = new KeyboardHookListener(new GlobalHooker());//init KeyboardHook
            m_KeyboardHookManager.Enabled = true;//init KeyboardHook

            m_MouseHookManager = new MouseHookListener(new GlobalHooker());//init KeyboardHook
            m_MouseHookManager.Enabled = true;//init KeyboardHook
        }

        private void Form3_Load(object sender, EventArgs e)
        {
            m_MouseHookManager.MouseClick += HookManager_MouseClick;
            m_KeyboardHookManager.KeyDown += HookManager_KeyDown;
            m_MouseHookManager.MouseMove += HookManager_MouseMove;
            //init KeyboardHook to these 3 func
            textBox1.Text = "0";
            textBox2.Text = "0";
            //engine.SetVariable("tessedit_char_whitelist", "0123456789,./\\%");
            //Read: 8 3 5
            while (sre.EndOfStream == false)
            {
                string text = sre.ReadLine();
                int m9 = Convert.ToInt32(text.ToCharArray()[0]) - 48;
                int m3 = Convert.ToInt32(text.ToCharArray()[2]) - 48;
                int mod = Convert.ToInt32(text.ToCharArray()[4]) - 48;
                module[m9, m3] = mod;
            }
        }



        async Task PutTaskDelay(int ms)//Delay function NET.  !!4.5+
        {
            try
            {
                await Task.Delay(ms);
            }
            catch (TaskCanceledException ex)
            {

            }
            catch (Exception ex)
            {

            }
        }

        private int CountSubstr(string strx, string substr)
        {
            return new Regex(Regex.Escape(substr)).Matches(strx).Count;
        }

        public Bitmap cropAtRect(Bitmap b, Rectangle r)
        {
            Bitmap nb = new Bitmap(r.Width, r.Height);
            Graphics g = Graphics.FromImage(nb);
            g.DrawImage(b, -r.X, -r.Y);
            return nb;
        }

        private Bitmap SetContrast(Bitmap oi, float contrast = 0.0f,float brightness = 0.0f,float gamma = 0.0f)
        {
            Bitmap adjustedImage = new Bitmap(oi.Width, oi.Height);
            contrast = contrast / 100f + 1f;
            brightness = brightness / 100f + 1f;
            gamma = gamma / 100f + 1f;
            float adjustedBrightness = brightness - 1.0f;
            // create matrix that will brighten and contrast the image
            float[][] ptsArray ={
                    new float[] {contrast, 0, 0, 0, 0}, // scale red
                    new float[] {0, contrast, 0, 0, 0}, // scale green
                    new float[] {0, 0, contrast, 0, 0}, // scale blue
                    new float[] {0, 0, 0, 1.0f, 0}, // don't scale alpha
                    new float[] {adjustedBrightness, adjustedBrightness, adjustedBrightness, 0, 1}};

            ImageAttributes imageAttributes = new System.Drawing.Imaging.ImageAttributes();
            imageAttributes.ClearColorMatrix();
            imageAttributes.SetColorMatrix(new ColorMatrix(ptsArray), ColorMatrixFlag.Default, ColorAdjustType.Bitmap);
            imageAttributes.SetGamma(gamma, ColorAdjustType.Bitmap);
            Graphics g = Graphics.FromImage(adjustedImage);
            g.DrawImage(oi, new Rectangle(0, 0, adjustedImage.Width, adjustedImage.Height), 0, 0, oi.Width, oi.Height, GraphicsUnit.Pixel, imageAttributes);
            return adjustedImage;
        }

        private Bitmap ResizeBitmap(Bitmap img2, int multiply)
        {
            Bitmap newImage = new Bitmap(multiply * img2.Size.Width, multiply * img2.Size.Height);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.SmoothingMode = SmoothingMode.HighQuality;
                gr.InterpolationMode = InterpolationMode.HighQualityBicubic;
                gr.PixelOffsetMode = PixelOffsetMode.HighQuality;
                gr.DrawImage(img2, new Rectangle(0, 0, multiply * img2.Size.Width, multiply * img2.Size.Height));
            }
            return newImage;
        }

        private Bitmap PrtScr(Rectangle rect)
        {
            Bitmap bmpScreenCapture = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(bmpScreenCapture);
            g.CopyFromScreen(Screen.PrimaryScreen.Bounds.X, Screen.PrimaryScreen.Bounds.Y, 0, 0, bmpScreenCapture.Size, CopyPixelOperation.SourceCopy);
            bmpScreenCapture = cropAtRect(bmpScreenCapture, rect);
            return bmpScreenCapture;
        }

        private string GSFF(int x, int y, int tx, int ty)
        {

            Rectangle rect = new Rectangle(x, y, tx - x, ty - y);
            Bitmap bmpScreenCapture = PrtScr(rect);
            bmpScreenCapture.Save(@".\sccp.png");
            Bitmap newImage = ResizeBitmap(bmpScreenCapture, 4);
            newImage = SetContrast(newImage, 20);
            newImage.Save(@".\sccp2.png");
            //Pix img = Pix.LoadFromFile(testImagePath);
            Page page = engine.Process(newImage);
            string text = page.GetText();
            newImage.Dispose();
            page.Dispose();
            FileW("z-out.txt", text);
            return text;

        }

        private Color GPX(int x, int y)
        {
            IntPtr hdc = GetDC(IntPtr.Zero);
            uint pixel = GetPixel(hdc, x, y);
            ReleaseDC(IntPtr.Zero, hdc);
            Color color = Color.FromArgb((int)(pixel & 0x000000FF),
                         (int)(pixel & 0x0000FF00) >> 8,
                         (int)(pixel & 0x00FF0000) >> 16);
            return color;
        }

        private bool TestCol(Color c, int r, int g, int b, int ap)
        {
            if (c.R < r + ap && c.R > r - ap)
            {
                if (c.G < g + ap && c.G > g - ap)
                {
                    if (c.B < b + ap && c.B > b - ap)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        async Task<bool> ChkName(string Name, int x, int y, int cx, int cy)
        {
            await PutTaskDelay(200);
            SetCursorPos(0, 0);//prevent popups
            await PutTaskDelay(200);
            return GSFF(x, y, cx, cy).Contains(Name);
        }

        private void CrSh(string inf)
        {
            FileW("zz-Last-Cresh.txt", inf);
            Environment.Exit(0);
        }

        private void FileW(string fname,string strx)
        {
            System.IO.StreamWriter file = new System.IO.StreamWriter(".\\" + fname);
            file.WriteLine(strx);
            file.Close();
        }
        




        private void HookManager_MouseClick(object sender, MouseEventArgs e)
        {
            //e.Button
            if (in_on == 1)
            {
                if (e.Button == MouseButtons.Left)
                {
                    mouse_c++;
                }
                if (mouse_c == 1)
                {
                    cx = e.X;
                    cy = e.Y;
                }
                if (mouse_c == 2)
                {
                    if (cx < e.X && cy < e.Y)
                    {
                        string textyyui = GSFF(cx, cy, e.X, e.Y);
                        textBox4.Text = textyyui.Replace("\n", Environment.NewLine);
                        mouse_c = 0;
                    }
                    else mouse_c = 0;
                }
            }
            else
            {
                mouse_c = 0;
            }
        }
        private void HookManager_KeyDown(object sender, KeyEventArgs e)//Add async for delay func!
        {
            //e.KeyCode
            /*int locx = gmx, locy = gmy;
            if (e.KeyCode == Keys.D1)
            {
                FileW("1.txt", GPX(locx, locy).R.ToString() + " " + GPX(locx, locy).G.ToString() + " " + GPX(locx, locy).B.ToString() + " - " + locx.ToString() + " " + locy.ToString());
            }
            if (e.KeyCode == Keys.D2)
            {
                FileW("2.txt", GPX(locx, locy).R.ToString() + " " + GPX(locx, locy).G.ToString() + " " + GPX(locx, locy).B.ToString() + " - " + locx.ToString() + " " + locy.ToString());
            }
            if (e.KeyCode == Keys.D3)
            {
                FileW("3.txt", GPX(locx, locy).R.ToString() + " " + GPX(locx, locy).G.ToString() + " " + GPX(locx, locy).B.ToString() + " - " + locx.ToString() + " " + locy.ToString());
            }
            if (e.KeyCode == Keys.D4)
            {
                FileW("4.txt", GPX(locx, locy).R.ToString() + " " + GPX(locx, locy).G.ToString() + " " + GPX(locx, locy).B.ToString() + " - " + locx.ToString() + " " + locy.ToString());
            }*/
            if (e.KeyCode == Keys.End)
            {
                Environment.Exit(0);
            }
        }
        private void HookManager_MouseMove(object sender, MouseEventArgs e)
        {
            //e.X, e.Y
            //pictureBox1.BackColor = GPX(e.X, e.Y);
            gmx = e.X;
            gmy = e.Y;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                mod9 = Convert.ToInt32(textBox1.Text);
                UpdateChangeModuleTypeRadios();
            }
            catch
            {
                textBox1.Text = "0";
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (Convert.ToInt32(textBox2.Text) > 3)
                {
                    textBox2.Text = "0";
                }
                else
                {
                    mod3 = Convert.ToInt32(textBox2.Text);
                    UpdateChangeModuleTypeRadios();
                }
            }
            catch
            {
                textBox2.Text = "0";
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton1.Checked == true)
            {
                ChangeModuleType(1);
            }
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton2.Checked == true)
            {
                ChangeModuleType(2);
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton4.Checked == true)
            {
                ChangeModuleType(4);
            }
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton3.Checked == true)
            {
                ChangeModuleType(3);
            }
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButton5.Checked == true)
            {
                ChangeModuleType(5);
            }
        }
        private void ChangeModuleType(int type)
        {
            module[mod9, mod3] = type;
        }
        private void UpdateChangeModuleTypeRadios()
        {
            if (module[mod9, mod3] == 1)
            {
                radioButton1.PerformClick();
            }
            else if (module[mod9, mod3] == 2)
            {
                radioButton2.PerformClick();
            }
            else if (module[mod9, mod3] == 3)
            {
                radioButton3.PerformClick();
            }
            else if (module[mod9, mod3] == 4)
            {
                radioButton4.PerformClick();
            }
            else if (module[mod9, mod3] == 5)
            {
                radioButton5.PerformClick();
            }
            else if (module[mod9, mod3] == 0)
            {
                radioButton1.Checked = false;
                radioButton2.Checked = false;
                radioButton3.Checked = false;
                radioButton4.Checked = false;
                radioButton5.Checked = false;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await PutTaskDelay(5000);
            Prepare();
        }


        private async Task RightClick(int xmm, int ymm, int sdelay = 1000)
        {
            await PutTaskDelay(sdelay);
            SetCursorPos(xmm, ymm);
            await PutTaskDelay(300);
            mouse_event(MOUSEEVENTF_RIGHTDOWN, 0, 0, 0, 0);
            await PutTaskDelay(100);
            mouse_event(MOUSEEVENTF_RIGHTUP, 0, 0, 0, 0);
        }
        private async Task LeftClick(int xmm, int ymm, int sdelay = 1000)
        {
            await PutTaskDelay(sdelay);
            SetCursorPos(xmm, ymm);
            await PutTaskDelay(300);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await PutTaskDelay(100);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        private async Task LeftDrag(int xf, int yf, int xt, int yt, int steps, int slowdown = 1000)
        {
            await PutTaskDelay(100);
            SetCursorPos(xf, yf);
            await PutTaskDelay(300);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
            await PutTaskDelay(slowdown);
            for (int i = 1; i <= steps; i++)
            {
                SetCursorPos((i * xt + (steps - i) * xf) / steps, (i * yt + (steps - i) * yf) / steps);
                await PutTaskDelay(10);
            }
            await PutTaskDelay(slowdown / 2);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
        }
        private string GetClipboard()
        {
            return Clipboard.GetText();
        }
        private bool GetMissionLocationSecurityIsHIsec()
        {
            string text = GetClipboard();
            int indx = text.IndexOf("This site contains special ship restrictions.");
            if (indx >= 0) return false;
            text = GetClipboard();
            indx = text.IndexOf("	Location");
            text = text.Remove(0, indx + 12);
            text = text.Remove(1);
            try{ if(Convert.ToInt32(text) >= 5) return true;}
            catch {CrSh("Cannot get mission information --IsLowSec");}
            return false;
        }

        private void Prepare()
        {
            #region
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            radioButton1.Enabled = false;
            radioButton2.Enabled = false;
            radioButton3.Enabled = false;
            radioButton4.Enabled = false;
            radioButton5.Enabled = false;
            button1.Enabled = false;
            #endregion 
            //Disable Buttons
            GamezOn();
        }
        private async Task<bool> AreActiveAll()//ADD FOR 3 MODULES PLS UPDATE !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!//NOT WORKING ASTEROID DIFFERENT NAMES SHIFTS :<
        {
            if (avaM == 1)
            {
                SetCursorPos(712, 140);
                await PutTaskDelay(500);
                return TestCol(GPX(712, 137), 255, 255, 255, 5);
            }
            if (avaM == 2)
            {
                SetCursorPos(696, 140);
                await PutTaskDelay(500);
                if (TestCol(GPX(696, 137), 255, 255, 255, 5))
                {
                    SetCursorPos(728, 140);
                    await PutTaskDelay(500);
                    if (TestCol(GPX(728, 137), 255, 255, 255, 5)) return true;
                }
            }
            return false;
        }
        private async Task<bool> Color4Chk(int[] x, int[] y, Color[] col, int n)
        {
            SetCursorPos(0, 0);
            await PutTaskDelay(500);
            bool isok = true;
            for (int i = 0; i < n; i++)
            {
                if (!TestCol(col[i], GPX(x[i], y[i]).R, GPX(x[i], y[i]).G, GPX(x[i], y[i]).B, 5))
                {
                    isok = false;
                }
                //FileW(Convert.ToString(i + 1) + ".txt", GPX(x[i], y[i]).R.ToString() + " " + GPX(x[i], y[i]).G.ToString() + " " + GPX(x[i], y[i]).B.ToString());
            }
            return isok;
        }
        private async Task doReplaceCrsa()
        {
            //chk inv is open
            SetCursorPos(0, 0);//prevent popups
            while (await ChkName("Inventor", 42, 94, 94, 108) == false)
            {
                await LeftClick(18, 131, 2000);
                SetCursorPos(0, 0);//prevent popups
                await PutTaskDelay(100);
                if (await ChkName("Inventor", 42, 94, 94, 108) == false) CrSh("\"Inventory\" not found --location --ChangeCrystalOperation");
            }
            //chk item hangar right position
            SetCursorPos(0, 0);//prevent popups
            if (await ChkName("Item hangar", 76, 496, 148, 515) == false) CrSh("\"Item hangar\" not found --location --ChangeCrystalOperation");
            //continue
            await LeftClick(112, 503);//select ItemHangar
            await PutTaskDelay(100);
            await LeftClick(665, 144);//select Filter
            SendKeys.Send("pyroxeres m");
            await PutTaskDelay(100);
            await LeftClick(989, 274);//open Fitting
            await PutTaskDelay(100);
            SetCursorPos(0, 0);
            await PutTaskDelay(2000);
            if (await ChkName("Ship Fitting", 253, 107, 321, 130) == false) CrSh("\"Ship Fitting\" not found --location --ChangeCrystalOperation");

            await LeftDrag(285, 116, 838, 116, 10, 200);//move fitting window

            InitItemVect();

            SetCursorPos(0, 0);
            await PutTaskDelay(1000);
            String row1str = GSFF(204, 233, 737, 247);
            int count = CountSubstr(row1str, "roxeres");
            if (count < 1) CrSh("\"Ship Fitting\" Not Enough Crystals --OutOfCrystals --ChangeCrystalOperation --part 1");

            await LeftDrag(item7[1], item5[1], 948, 250, 10, 200);

            await RightClick(741, 297, 100);
            await LeftClick(776, 353, 100);
            await PutTaskDelay(200);

            SetCursorPos(0, 0);
            await PutTaskDelay(1000);
            row1str = GSFF(204, 233, 737, 247);
            count = CountSubstr(row1str, "roxeres");
            if (count < 1) CrSh("\"Ship Fitting\" Not Enough Crystals --OutOfCrystals --ChangeCrystalOperation --part 2");

            await LeftDrag(item7[1], item5[1], 977, 232, 10, 200);

            await RightClick(838, 118);
            await LeftClick(870, 142);//close the fitting window

            await LeftClick(745, 102, 2000);//finally close the inventory window

            crsa = 0;

        }
        private async Task GetTotalBelts()
        {
            await LeftClick(890,174);
            await SortBy(2);
            SetCursorPos(0, 0);
            await PutTaskDelay(1000);
            String row1str = GSFF(905, 208, 1014, 581);
            maxBelt = CountSubstr(row1str, "Asteroid");
        }
        private async Task SortBy(int by)
        {
            await LeftClick(784, 200);
            if (by == 1) await LeftClick(819, 197);
            if (by == 2) await LeftClick(872, 199);
            if (by == 3) await LeftClick(932, 197);
        }
        private async Task WarpBelt(int belt)
        {
            await LeftClick(890, 174);
            await SortBy(2);
            await LeftClick(805, 220 + 19 * (belt - 1));
            await LeftClick(821, 90);/////////////////////////WARP
            await PutTaskDelay(1000);
            //adjusting camera
            if (await IsWarping())
            {
                await PutTaskDelay(10000);
                await LeftDrag(681, 0, 681, 500, 10, 100);
            }
            while (await IsWarping())
            {
                await PutTaskDelay(5000);
                GC.Collect();
                await PutTaskDelay(1000);
            }
        }
        private async Task RecDrones()
        {
            await RightClick(859, 651);
            await LeftClick(900, 694);
            //while(await ChkDrones())
            {
                await PutTaskDelay(1000);
            }
        }
        private async Task doDock()
        {
            await PutTaskDelay(1000);
            await LeftClick(933, 174);
            await SortBy(2);
            await LeftClick(877, 237);
            await LeftClick(835, 86);
            while (await IsInSpace())
            {
                await PutTaskDelay(5000);
                GC.Collect();
                await PutTaskDelay(1000);
            }
            await PutTaskDelay(3000);
        }
        private async Task<bool> IsInSpace()
        {
            await PutTaskDelay(1000);
            return await ChkName("Selected Item", 771, 1, 854, 19);
        }
        private async Task Panic(string pni)
        {
            await RecDrones();
            await doDock();
            CrSh(pni);
        }
        private async Task CountAvaM()
        {
            await PutTaskDelay(100);
            avaM = 0;
            for (int i = 0; i <= 2; i++)
            {
                if (ModulesV[i] == 1) avaM++;
            }
        }
        private async Task CheckCrystalInSpace()//pr4
        {
            await PutTaskDelay(200);
            SetCursorPos(616,622);
            await PutTaskDelay(2000);
            String row1str = GSFF(534,494, 625,534);
            if (CountSubstr(row1str, "xeres") == 1) ModulesV[0] = 1;
            else ModulesV[0] = 0;
            await PutTaskDelay(200);
            SetCursorPos(667, 622);
            await PutTaskDelay(2000);
            row1str = GSFF(585, 494, 676, 534);
            if (CountSubstr(row1str, "xeres") == 1) ModulesV[1] = 1;
            else ModulesV[1] = 0;
            await PutTaskDelay(200);
            SetCursorPos(718, 622);
            await PutTaskDelay(2000);
            row1str = GSFF(636, 494, 727, 534);
            if (CountSubstr(row1str, "xeres") == 1) ModulesV[2] = 1;
            else ModulesV[2] = 0;
            await CountAvaM();
            if (avaM == 0)
            {
                isRestarting = 1;
                crsa = 1;
            }
        }
        private async Task AsteroidTab()
        {
            await LeftClick(968, 174);
            await SortBy(1);
        }
        private async Task<bool> IsWarping()
        {
            SetCursorPos(493, 720);
            await PutTaskDelay(1000);
            String row1str = GSFF(412, 660, 603, 691);
            int count = CountSubstr(row1str, "You cannot");
            if (count == 1) return true;
            return false;
        }
        private async Task LaunchDrones()//NO CHECKING !!!
        {
            await RightClick(862, 626);
            await LeftClick(895, 636);
            await PutTaskDelay(7500);
        }
        private async Task<bool> ChkDrones()
        {
            SetCursorPos(0, 0);
            await PutTaskDelay(1000);
            String text = GSFF(809, 642, 970, 659);
            int indx = text.IndexOf("(");
            text = text.Remove(0, indx + 1);
            text = text.Remove(1);
            try
            {
                if (Convert.ToInt32(text) >= 5) return true;
            }
            catch
            {
                //CrSh("Could not check drones!");//Recover Drones is ChkDrones() calling resulting in frequent crash!!!
                return false;
            }
            return false;
        }
        private async Task<bool> IsTargetApplied()
        {
            /*Clipboard.Clear();
            await RightClick(714, 47);//r clik
            await LeftClick(762, 120, 100);//l clik
            await LeftClick(834, 13, 100);//selected item
            await RightClick(866, 220);//r clik2
            await LeftClick(898, 230, 100);//l clik2
            await PutTaskDelay(500);
            String text = Clipboard.GetText();
            if (text.IndexOf("The most common ore") >= 0)
            {
                await LeftClick(901, 11, 100);
                return true;
            }
            else
            {
                await LeftClick(446, 699, 100);
                return false;
            }*/
            int[] x = new int[4], y = new int[4];
            int n = 4;
            Color[] col = new Color[4];
            col[0] = Color.FromArgb(73, 64, 33);
            x[0] = 701;
            y[0] = 56;
            col[1] = Color.FromArgb(242, 241, 226);
            x[1] = 705;
            y[1] = 41;
            col[2] = Color.FromArgb(204, 191, 177);
            x[2] = 714;
            y[2] = 41;
            col[3] = Color.FromArgb(197, 187, 163);
            x[3] = 725;
            y[3] = 49;
            return await Color4Chk(x, y, col, n);
        }
        private async Task<bool> ChkForAst()
        {
            return await ChkName("Asteroid", 845, 208, 895, 224);
        }
        private async Task<int> GetDistFromClipboard()
        {
            await PutTaskDelay(500);
            String text = Clipboard.GetText();
            text = text.Remove(0, text.IndexOf("Distance: ") + 10);
            text = text.Remove(text.IndexOf("m"));
            if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
            if (text.IndexOf("k") >= 0)
            {
                text = text.Remove(text.IndexOf(" k"));
                return Convert.ToInt32(text) * 1000;
            }
            else
            {
                return Convert.ToInt32(text);
            }
        }
        private async Task LockAst(int rangem)
        {
            await LeftClick(805, 220);//re-select asteroid!
            bool inRange = false;
            while (inRange == false)
            {
                Clipboard.Clear();
                await LeftClick(794, 83);
                await RightClick(823, 36);
                await LeftClick(855, 46);
                await PutTaskDelay(500);
                //if (await GetDistFromClipboard() >= 30000)
                {
                    //isRestarting = 1;
                    //return;
                }
                if (await GetDistFromClipboard() <= rangem) inRange = true;
            }
            await LeftClick(446, 699);
            await LeftClick(805, 220);
            await LeftClick(922, 82);
            await PutTaskDelay(3000);
        }
        private async Task EngageModuluz()
        {
            if (ModulesV[0] == 1) await LeftClick(616, 622);
            if (ModulesV[1] == 1) await LeftClick(667, 622);
            if (ModulesV[2] == 1) await LeftClick(718, 622);
        }
        private String StrRemoveR(String strxc, String charx)
        {
            while (strxc.IndexOf(charx) >= 0)
            {
                strxc = strxc.Replace(charx, "");
            }
            return strxc;
        }
        private async Task<bool> IsCargoFull()
        {
            await LeftClick(356, 636);
            await LeftClick(236, 309);
            SetCursorPos(0, 0);
            await PutTaskDelay(1500);
            /*String text = GSFF(379,256, 523,272);
            int indx = text.IndexOf("/");
            //text = text.Remove(0, indx + 1);
            text = text.Remove(indx);
            text = StrRemoveR(text, "\n");
            text = StrRemoveR(text, " ");
            text = StrRemoveR(text, ".");
            text = StrRemoveR(text, ",");*/
            bool bl;
            bl = TestCol(GPX(606, 267), 5, 68, 88, 6);
            await LeftClick(728, 222, 100);
            return bl;
            //return Convert.ToInt32(text) >= 270000;
        }
        private async Task UndockPatchBug()
        {
            await LeftDrag(764, 11, 802, 11, 100);
        }
        private async Task MineAlg()
        {
            if (!await IsTargetApplied())
            {
                if (!await ChkForAst())
                {
                    isRestarting = 1;
                    currentBelt++;
                }
            }
            while (isRestarting == 0)
            {
                GC.Collect();
                if (isRestarting == 0)
                {
                    while (!await IsTargetApplied()) await LockAst(14000);
                    if (await IsTargetApplied() && isRestarting == 0)
                    {
                        await EngageModuluz();
                        while (!await IsTargetApplied()) await LockAst(14000);
                    }
                    //while
                    if (isRestarting == 0) await PutTaskDelay(CyDu * 1000);
                    //endwhile
                    
                    //isRestarting = 1;
                    //currentBelt++;
                    await CheckCrystalInSpace();
                    if (await IsCargoFull()) isRestarting = 1;
                    if (!await ChkForAst())
                    {
                        currentBelt++;
                        isRestarting = 1;
                    }
                    if (isRestarting == 0) await LeftClick(805, 220);//re-select asteroid!
                }
            }
        }
        private async Task MINE()
        {
            ModulesV[0] = 1;
            ModulesV[1] = 1;
            ModulesV[2] = 0;
            //crsa = 1;
            while(true)
            {
                GC.Collect();
                noaddcounter = 0;
                isRestarting = 0;
                if (currentBelt == maxBelt + 1) CrSh("EndOfLineNigga");
                await RepairShip();
                if (crsa == 1) await doReplaceCrsa();//if(crsa==1)
                await Undock();
                await UndockPatchBug();
                if (isFirst == 1) await GetTotalBelts();
                await WarpBelt(currentBelt);
                await LaunchDrones();
                if (!await ChkDrones()) await Panic("Drones Panic !!! not in space? or Could Not read?");
                await AsteroidTab();
                await CountAvaM();
                await CheckCrystalInSpace();
                await MineAlg();
                await RecDrones();
                await doDock();
                await TransferItems3();
                isFirst = 0;
                counter++;
            }
        }
        private async void GamezOn()
        {
            //string textyyui = GSFF();
            //await AcceptMission();
            //await TransferItems2();
            //await RepairShip();
            //await Undock();
            //await GetDest();
            //await WarpDed();
            //await GoHome();
            await MINE();
        }
        private async Task AcceptMission()
        {
            //check window IsOpen
            if (await ChkName("Agent Conversation", 84, 46, 188, 60) == false)
            {
                await RightClick(866, 499);
                await PutTaskDelay(2000);
                await LeftClick(887, 526);
                await PutTaskDelay(1000);
                //chk window open
                if (await ChkName("Agent Conversation", 84, 46, 188, 60) == false)
                {
                    CrSh("\"Agent Conversation\" --location --position");
                }
            }
            //LeftDrag(508, 114, 858, 515, 100); Window Pos: 1277,288
            //check completion
            if (!await ChkName("Accept", 508, 593, 905, 615))
            {
                await LeftClick(652, 601);//Complete
                await LeftClick(652, 601);//Request
            }
            //await LeftClick(652, 601);//Complete
            //await LeftClick(652, 601);//Request
            await RightClick(726, 116);
            await PutTaskDelay(1000);
            await LeftClick(750, 141);//Copy All Mission Info
            await PutTaskDelay(500);
            while (GetMissionLocationSecurityIsHIsec() == false)
            {
                await LeftClick(700, 600);
                await PutTaskDelay(500);
                await LeftClick(226, 604);
                await PutTaskDelay(500);
                await RightClick(726, 116);
                await PutTaskDelay(1000);
                await LeftClick(750, 141);//Copy All Mission Info
                await PutTaskDelay(500);
                if (GetMissionLocationSecurityIsHIsec() == false) CrSh("Low Sec Mission Error x2");
            }
            await PutTaskDelay(500);
            await LeftClick(652, 601);
            await PutTaskDelay(1000);
            await LeftClick(805, 601);//close window
            await PutTaskDelay(1000);
        }
        private async Task PrepareUndock()//Repair Ship & Transfer Items
        {

        }
        private async Task WarpDed()
        {
            await LeftClick(111, 163);
            await LeftClick(138, 280);
            await Modules(3);
            do
            {
                //await PutTaskDelay(70000);
                await UpdateObj();
                while (cobj.IndexOf("You need to warp ") >= 0)
                {
                    await UpdateObj();
                }
                await PutTaskDelay(10000);
                //await Modules(2);
                await ExecTargets();
                await GetDestructibles();
                await WreckTargets();
                await GetObjective();
            } while (continuegate);
        }

        private async Task UpdateObj()
        {
            await LeftClick(90, 163, 100);
            await RightClick(175, 162);
            await LeftClick(207, 172);
            await PutTaskDelay(100);
            SetCursorPos(0, 0);
            await PutTaskDelay(100);
            cobj = Clipboard.GetText();
            await PutTaskDelay(100);
        }

        private async Task GetDestructibles()
        {
            await LeftClick(90, 163, 100);
            await RightClick(175, 162);
            await LeftClick(207, 172);
            await PutTaskDelay(100);
            if (Clipboard.GetText().IndexOf("You need to destroy ") >= 0)//  Destroy Structure
            {
                await LeftClick(906, 106, 100);
                await PutTaskDelay(3000);
                await Modules(1);
                while (Clipboard.GetText() != "")
                {
                    await RightClick(817, 51, 100);
                    await LeftClick(849, 61, 100);
                    await PutTaskDelay(100);
                }
            }
            else
            {
                //continue
            }
        }

        private async Task GetObjective()
        {
            await LeftClick(90, 163, 1000);
            await RightClick(175, 162);
            await LeftClick(207, 172);
            await PutTaskDelay(100);
            continuegate = false;
            if (Clipboard.GetText() == "You need to activate the Acceleration Gate" || Clipboard.GetText().IndexOf("Confiscated Viral Agent</a> in your cargohold") >= 0)
            {
                await LeftClick(915, 177, 100);
                await LeftClick(805, 220, 100);
                await LeftClick(768, 101);
                #region
                int dist = 0;
                await RightClick(817, 51, 100);
                await LeftClick(849, 61, 100);
                await PutTaskDelay(100);
                string text = Clipboard.GetText();
                text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                text = text.Remove(text.IndexOf("m"));
                if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                if (text.IndexOf("k") >= 0)
                {
                    text = text.Remove(text.IndexOf(" k"));
                    dist = Convert.ToInt32(text) * 1000;
                }
                else
                {
                    dist = Convert.ToInt32(text);
                }
                while (dist >= 2000)//Get dist
                {
                    await RightClick(817, 51, 100);
                    await LeftClick(849, 61, 100);
                    await PutTaskDelay(100);
                    text = Clipboard.GetText();
                    text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                    text = text.Remove(text.IndexOf("m"));
                    if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                    if (text.IndexOf("k") >= 0)
                    {
                        text = text.Remove(text.IndexOf(" k"));
                        dist = Convert.ToInt32(text) * 1000;
                    }
                    else
                    {
                        dist = Convert.ToInt32(text);
                    }
                }
                #endregion
                //Get Dist
                await LeftClick(821, 105);
                continuegate = true;
            }
            else if (Clipboard.GetText().IndexOf("Mission Complete") >= 0 || Clipboard.GetText().IndexOf("Bring ") >= 0)
            {
                //Go Home Restart
                //DO NONE > EXIT
                await PutTaskDelay(1000);
                SetCursorPos(0, 0);
                await PutTaskDelay(1000);
            }
            else
            {
                //PANIC! NOT REGISTERED OBJECTIVE > RUN AWAY!
                await PutTaskDelay(120000);
            }
        }

        private async Task GetEnemy(int xt, int yt)
        {
            await LeftClick(xt, yt, 100);
            await RightClick(817, 51, 100);
            await LeftClick(849, 61, 100);
        }

        private async Task AllTargets()
        {
            for (int i = 1; i <= 12; i++)
            {
                await TargetExist(i);
                textBox3.Text = Clipboard.GetText();
                vect[i] = Clipboard.GetText();
            }
        }

        private async Task LockTgt(int m9)
        {
            await LeftClick(804, ((12 - m9) * 220 + (m9 - 1) * 431) / 11, 100);
            await LeftClick(906, 106);
        }

        private async Task Att()
        {
            bool end = false;
            while (end == false)
            {
                await LeftClick(697, 62, 100);
                await RightClick(817, 51, 100);
                await LeftClick(849, 61, 100);
                await PutTaskDelay(100);
                if (Clipboard.GetText() == "")
                {
                    end = true;
                }
                else
                {
                    //start orbit
                    await LeftClick(838, 103, 100);
                    //launch dornes
                    await LeftDrag(821, 480, 841, 504, 50, 5);
                    await PutTaskDelay(6000);
                    //eng drones
                    await RightClick(841, 504, 100);
                    await LeftClick(873, 514, 100);
                    try
                    {
                        #region
                        int dist = 0;
                        //while (dist <= 25000)//Get dist
                        bool actmodules = false, appgate = false;
                        for (int jk = 1; jk <= 20; jk++)
                        {
                            await RightClick(817, 51, 100);
                            await LeftClick(849, 61, 100);
                            string text = Clipboard.GetText();//Exceptional Case Drone Kills First Result: Break!
                            text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                            text = text.Remove(text.IndexOf("m"));
                            if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                            if (text.IndexOf("k") >= 0)
                            {
                                text = text.Remove(text.IndexOf(" k"));
                                dist = Convert.ToInt32(text) * 1000;
                            }
                            else
                            {
                                dist = Convert.ToInt32(text);
                            }
                            if (dist >= 100000 && appgate == false)//approach gate
                            {
                                appgate = true;
                                await LeftClick(915, 177, 100);
                                await LeftClick(769, 200, 100);
                                await LeftClick(805, 220, 100);
                                await LeftClick(768, 101, 100);
                                await LeftClick(824, 179, 100);
                                await LeftClick(842, 202, 100);
                                jk = 21;
                                end = true;
                            }
                            if (dist >= 30000 && dist <= 70000 && actmodules == false)
                            {
                                actmodules = true;
                                await Modules(1);
                            }
                        }
                        #endregion
                    }
                    catch
                    {
                        //Drones Killed First, Continue!
                    }
                    //Get Dists
                }
                await PutTaskDelay(100);
                Clipboard.Clear();
                await RightClick(817, 51, 100);
                await LeftClick(849, 61, 100);
                await PutTaskDelay(100);
                int jk2 = 0;
                while (Clipboard.GetText() != "" && jk2 <= 150 && end == false)
                {
                    await RightClick(817, 51, 100);
                    await LeftClick(849, 61, 100);
                    await PutTaskDelay(100);
                    jk2++;
                    label3.Text = Convert.ToString(jk2);
                }
                if (jk2 >= 150)//Stuck!
                {
                    end = true;
                }
                //ret drones
                await LeftDrag(841, 504, 821, 480, 50, 5);
                await PutTaskDelay(10000);
            }
        }
        
        private async Task ExecTargets()
        {
            zeroleft = true;
            while (zeroleft)
            {
                bool moretargets = true;
                bool accelgate = false;
                int numtargets = 0;
                await TargetExist(1);
                if (Clipboard.GetText().IndexOf("Acceleration Gate<br>") == 0)
                {
                    accelgate = true;
                }
                while (moretargets)
                {
                    await LeftClick(842, 202, 10);//Dist sort
                    for (int i = 1; i <= 6; i++)//6 tgts for speed !
                    {
                        await TargetExist(i, false, true);
                        if (Clipboard.GetText().IndexOf("- Star") > 0)
                        {
                            moretargets = false;
                            i = 13;
                        }
                        else
                        {
                            numtargets++;
                        }
                    }
                    if (accelgate == true)
                    {
                       //numtargets--;
                    }
                    if (numtargets == 0) zeroleft = false;
                    while (numtargets != 0)
                    {
                        for (int i = 1; i <= maxlocktgt; i++)
                        {
                            /*if (accelgate == true)
                            {
                                if (i + 1 <= maxlocktgt)
                                {
                                    await LockTgt(i + 1);
                                    numtargets--;
                                }
                            }
                            else
                            {*/
                                await LockTgt(i);
                                numtargets--;
                            //}
                            if (numtargets == 0) i = maxlocktgt + 1;
                        }
                        //Engage!
                        await PutTaskDelay(20000);
                        await Att();
                    }
                    await LeftClick(769, 200, 10);//Icon Sort
                }
            }
        }

        private async Task WreckTargets()
        {
            bool moretargets = true;
            int numtargets = 0;
            while (moretargets)
            {
                for (int i = 1; i <= 12; i++)// get only first 10 wrecks >speed
                {
                    await TargetExist(i, true);
                    if (Clipboard.GetText().IndexOf("- Star") > 0)
                    {
                        moretargets = false;
                        i = 13;
                    }
                    else
                    {
                        numtargets++;
                    }
                }
                while (numtargets != 0)
                {
                    for (int i = 1; i <= maxlocktgt; i++)
                    {
                        await LockTgt(i);
                        numtargets--;
                        if (numtargets == 0) i = maxlocktgt + 1;
                    }
                    //Engage!
                    await PutTaskDelay(10000);
                    await Wreck();
                }
            }
        }

        private async Task Wreck()
        {
            bool end = false;
            while (end == false)
            {
                await LeftClick(697, 62, 100);
                await RightClick(817, 51, 100);
                await LeftClick(849, 61, 100);
                await PutTaskDelay(100);
                if (Clipboard.GetText() == "")
                {
                    end = true;
                }
                else
                {
                    //Finally Approach
                    await LeftClick(768, 101, 100);
                    int dist = 0;
                    #region
                    await RightClick(817, 51, 100);
                    await LeftClick(849, 61, 100);
                    await PutTaskDelay(100);
                    string text = Clipboard.GetText();
                    text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                    text = text.Remove(text.IndexOf("m"));
                    if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                    if (text.IndexOf("k") >= 0)
                    {
                        text = text.Remove(text.IndexOf(" k"));
                        dist = Convert.ToInt32(text) * 1000;
                    }
                    else
                    {
                        dist = Convert.ToInt32(text);
                    }
                    while (dist >= 47000)//Get dist
                    {
                        await RightClick(817, 51, 100);
                        await LeftClick(849, 61, 100);
                        await PutTaskDelay(100);
                        text = Clipboard.GetText();
                        text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                        text = text.Remove(text.IndexOf("m"));
                        if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                        if (text.IndexOf("k") >= 0)
                        {
                            text = text.Remove(text.IndexOf(" k"));
                            dist = Convert.ToInt32(text) * 1000;
                        }
                        else
                        {
                            dist = Convert.ToInt32(text);
                        }
                    }
                    await Modules(5);
                    while (dist >= 5000)//Get dist
                    {
                        await RightClick(817, 51, 100);
                        await LeftClick(849, 61, 100);
                        await PutTaskDelay(100);
                        text = Clipboard.GetText();
                        text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                        text = text.Remove(text.IndexOf("m"));
                        if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                        if (text.IndexOf("k") >= 0)
                        {
                            text = text.Remove(text.IndexOf(" k"));
                            dist = Convert.ToInt32(text) * 1000;
                        }
                        else
                        {
                            dist = Convert.ToInt32(text);
                        }
                    }
                    await Modules(4);
                    while (dist >= 2000)//Get dist
                    {
                        await RightClick(817, 51, 100);
                        await LeftClick(849, 61, 100);
                        await PutTaskDelay(100);
                        text = Clipboard.GetText();
                        text = text.Remove(0, text.IndexOf("Distance: ") + 10);
                        text = text.Remove(text.IndexOf("m"));
                        if (text.IndexOf(",") >= 0) text = text.Remove(text.IndexOf(","), 1);
                        if (text.IndexOf("k") >= 0)
                        {
                            text = text.Remove(text.IndexOf(" k"));
                            dist = Convert.ToInt32(text) * 1000;
                        }
                        else
                        {
                            dist = Convert.ToInt32(text);
                        }
                    }
                    //Loot all
                    await LeftClick(821, 105, 1000);
                    await LeftClick(523, 650, 1000);
                    await LeftClick(918, 232, 1000);
                    #endregion
                    //Get Dists
                }
                await PutTaskDelay(100);
                while (Clipboard.GetText() != "")
                {
                    await RightClick(817, 51, 100);
                    await LeftClick(849, 61, 100);
                    await PutTaskDelay(100);
                }
            }
        }

        private async Task TargetExist(int m9, bool wreck = false, bool nogate = false)
        {
            Clipboard.Clear();
            await LeftClick(876, 176, 100);
            if (nogate == true) await LeftClick(769, 200, 10);//Icon Sort
            await GetEnemy(805, 220);
            if (wreck == false)
            {
                if (nogate == false)
                {
                    await LeftClick(915, 177, 100);
                }
                else
                {
                    await LeftClick(824, 179, 100);
                    await LeftClick(842, 202, 10);//Dist sort
                }
            }
            else await LeftClick(955, 179, 100);
            await GetEnemy(804, ((12 - m9) * 220 + (m9 - 1) * 431) / 11);
            await PutTaskDelay(100);
        }

        private async Task ActModule(int m3, int m9)
        {
            if (m3 == 1)
            {
                await LeftClick(((8 - m9) * 634 + (m9 - 1) * 991) / 7, 625, 100);
            }
            if (m3 == 2)
            {
                await LeftClick(((8 - m9) * 658 + (m9 - 1) * 1017) / 7, 668, 100);
            }
            if (m3 == 3)
            {
                await LeftClick(((8 - m9) * 634 + (m9 - 1) * 991) / 7, 712, 100);
            }
        }

        private async Task Modules(int tpe)//1=attack 2=nonw 3=w 4=salv 5=tract
        {
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= 8; j++)
                {
                    try
                    {
                        if (module[j, i] == tpe) await ActModule(i, j);
                    }
                    catch
                    {

                    }
                }
            }
        }
        private async Task Undock()
        {
            await LeftClick(948, 174);
            /*while(!await IsInSpace())
            {
                await PutTaskDelay(200);
            }*/
            //UNDOCK PATCH BUG!!!
            await PutTaskDelay(15000);
        }
        private async Task GetDest()//forward!
        {
            await LeftClick(111, 163);
            await LeftClick(138, 280);
            await LeftClick(876, 176);
            while (await ChkName("Jump", 121, 80, 166, 98))
            {
                await RightClick(143, 109, 2000);
                await LeftClick(175, 119, 20);
                await PutTaskDelay(5000);
            }
        }
        private async Task GoHome()
        {
            await LeftClick(111, 163);
            await LeftClick(121, 220);
            Clipboard.Clear();
            await LeftClick(915, 177);
            while (Clipboard.GetText() == "")
            {
                Clipboard.Clear();
                await RightClick(143, 109, 7000);
                await LeftClick(175, 119);
                await RightClick(806,315);
                await LeftClick(838, 325);
                await PutTaskDelay(100);
            }
            await LeftClick(908, 14);
            await LeftClick(821, 105);
            await PutTaskDelay(120000);
        }
        private async Task RepairShip()
        {
            await RightClick(527, 367, 100);
            await LeftClick(569, 410, 100);
            await RightClick(627, 292);
            Clipboard.Clear();
            await LeftClick(659, 302, 100);
            await PutTaskDelay(200);
            if (GetClipboard().IndexOf("<b>0 ISK</b>") < 0)
            {
                //Repair
                await LeftClick(556, 756);
                await LeftClick(488, 481);
            }
            await LeftClick(703, 247, 100);
        }
        private async Task TransferItems()
        {
            await LeftClick(18, 131, 2000);
            await LeftClick(115, 142);
            await PutTaskDelay(1000);
            InitItemVect();
            //Move each item
            #region 
            bool okexit = false;
            while (okexit == false)
            {
                await RightClick(741, 297);
                await LeftClick(776, 353);
                await PutTaskDelay(1000);
                for (int i = 1; i <= 4; i++)// 5th row problem ! so <= 4
                {
                    for (int j = 1; j <= 7; j++)
                    {
                        Clipboard.Clear();
                        await RightClick(item7[j], item5[i], 100);
                        await LeftClick(item7[j] + 32, item5[i] + 10, 100);
                        await RightClick(741, 297, 100);
                        await LeftClick(773, 307, 100);
                        await LeftClick(908, 14, 100);
                        int ckg = CheckItemGood();
                        if (ckg == 0)
                        {
                            //Move to Hangar
                            await LeftDrag(item7[j], item5[i], 119, 463, 10);
                        }
                        if(ckg==2)
                        {
                            okexit = true;
                            i = 6;
                            j = 8;
                        }
                    }
                }
            }
            #endregion
            await LeftClick(18, 131, 2000);
        }
        private async Task TransferItems2()
        {
            //chk inv is open
            SetCursorPos(0, 0);//prevent popups
            while (await ChkName("Inventory", 42, 94, 94, 108) == false)
            {
                await LeftClick(18, 131, 2000);
                SetCursorPos(0, 0);//prevent popups
                await PutTaskDelay(1000);
                if (await ChkName("Inventory", 42, 94, 94, 108) == false) CrSh("\"Inventory\" not found --location --TransferItems");
            }
            //chk item hangar right position
            SetCursorPos(0, 0);//prevent popups
            if (await ChkName("Item hangar", 77, 448, 153, 465) == false) CrSh("\"Item hangar\" not found --location --TransferItems");
            //continue
            await LeftClick(115, 142);//select ship cargo
            await PutTaskDelay(1000);
            //plan1
            //stack
            InitItemVect();
            bool hasend = true;
            //take all items before Imperial
            //stack
            #region
            while (hasend)
            {
                await RightClick(741, 297, 100);
                await LeftClick(776, 353, 100);
                await PutTaskDelay(200);
                String row1str = GSFF(204, 233, 737, 247);
                int count = CountSubstr(row1str, "Imperial");
                //Move to Hangar
                if (count != 7) await LeftDrag(204, 233, 76 * (7 - count) + 204, 247, 10, 100);
                if (count != 7) await LeftDrag(item7[1], item5[1], 119, 463, 10, 100);
                if (count == 7) hasend = false;
            }
            //take all after Imperial
                hasend = true;
                int crow = 1;// set crow=2 to prevent impossible happen
                while (hasend)//skipping Imperial lines
                {
                    crow++;
                    await RightClick(741, 297, 100);
                    await LeftClick(776, 353, 100);
                    await PutTaskDelay(200);
                    String row1str = GSFF(204, 233 + 106 * (crow - 1), 737, 247 + 106 * (crow - 1));
                    int count = CountSubstr(row1str, "Imperial");
                    if (count != 7) hasend = false;
                }
            hasend = true;
            while(hasend)//take the rest
            {
                await RightClick(741, 297, 100);
                await LeftClick(776, 353, 100);
                await PutTaskDelay(200);
                for (int i = crow; i <= 5; i++) if (hasend && i <= crow + 1)//i <= 3
                    {
                        String row1str = GSFF(204, 233 + 106 * (i - 1), 737, 247 + 106 * (i - 1));
                        int count = CountSubstr(row1str, "Imperial");
                        if (i == crow)
                        {
                            SetCursorPos(item7[count + 1], item5[i]);
                            await PutTaskDelay(200);
                            Color testc = GPX(214 + 76 * count, 245 + 106 * (i - 1));
                            if (!TestCol(testc, 51, 41, 22, 5))
                            {
                                hasend = false;
                            }
                        }
                        await PutTaskDelay(200);
                        if (hasend && i <= crow + 1)//setting second i to 5
                        {
                            await LeftDrag(204 + 76 * count, 233 + 106 * (i - 1), 737, 247 + 106 * (5 - 1), 10, 100);//setting second i to 5
                            /*if (count != 7)*/
                            await LeftDrag(item7[count + 1], item5[i], 119, 463, 10, 100);
                        }
                    }
            }
            #endregion

            await LeftClick(18, 131, 2000);//finally close tha window



        }
        private async Task TransferItems3()//for mining purpose//also checking for drones to be 5
        {
            //chk inv is open
            SetCursorPos(0, 0);//prevent popups
            while (await ChkName("Inventor", 42, 94, 94, 108) == false && await ChkName("Inventorg", 42, 94, 94, 108) == false)
            {
                await LeftClick(18, 131, 2000);
                SetCursorPos(0, 0);//prevent popups
                await PutTaskDelay(1000);
                if (await ChkName("Inventor", 42, 94, 94, 108) == false && await ChkName("Inventorg", 42, 94, 94, 108) == false) CrSh("\"Inventory\" not found --location --UnloadOperation");
            }
            //chk item hangar right position
            SetCursorPos(0, 0);//prevent popups
            if (await ChkName("Item hangar", 76, 496, 148, 515) == false) CrSh("\"Item hangar\" not found --location --UnloadOperation");
            //continue
            await LeftClick(115, 186);//select Ore Hold
            await RightClick(741, 297);
            await LeftClick(773, 307);//select all
            await PutTaskDelay(1000);
            
            InitItemVect();

            await LeftDrag(item7[1], item5[1], 118, 503, 10, 200);

            await LeftClick(115, 142);//select Cargo Hold
            await RightClick(741, 297);
            await LeftClick(773, 307);//select all
            await PutTaskDelay(1000);

            await LeftDrag(item7[1], item5[1], 118, 503, 10, 200);

            await LeftClick(114, 165);//select Drone Bay

            while (await ChkName("5", 618, 695, 751, 714) == false)// add more drones!
            {
                await LeftClick(112, 503);//select ItemHangar
                await PutTaskDelay(100);
                await LeftClick(665, 144);//select Filter
                await PutTaskDelay(100);
                SendKeys.Send("acolyte II");
                await PutTaskDelay(1000);
                await LeftDrag(item7[1], item5[1], 114, 165, 10, 200);
                await PutTaskDelay(100);
                await LeftClick(516, 478);
                await PutTaskDelay(100);
                await LeftClick(495, 464);
                await PutTaskDelay(100);
                await LeftClick(114, 165);//select Drone Bay
                await PutTaskDelay(1000);
            }
            
                


            
            await LeftClick(745, 102, 2000);//finally close inventory window






        }
        private void InitItemVect()
        {
            item5[1] = 208;
            item5[2] = 297;
            item5[3] = 421;
            item5[4] = 524;
            item5[5] = 624;
            item7[1] = 248;
            item7[2] = 325;
            item7[3] = 402;
            item7[4] = 475;
            item7[5] = 553;
            item7[6] = 629;
            item7[7] = 704;
        }
        private int CheckItemGood()
        {
            Clipboard.Clear();
            string text = GetClipboard();
            int indx = text.IndexOf("The delicate crystalline structures");
            if (text == "") return 2;
            if (indx <= 0) return 0;
            else return 1;
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (in_on == 0)
            {
                in_on = 1;
                button2.Text = "Loaded";
            }
            else
            {
                in_on = 0;
                button2.Text = "Load";
            }
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {

        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }

        private void textBox5_TextChanged(object sender, EventArgs e)
        {
            try
            {
                currentBelt = Convert.ToInt32(textBox5.Text.ToString());
            }
            catch
            {

            }
        }
    }
}
