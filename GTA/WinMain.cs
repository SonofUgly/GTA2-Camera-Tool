using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Fasm;
namespace GTA
{
    public partial class Camera : Form
    {
        #region DLL Imports
        private const int PROCESS_ALL_ACCESS = 0x1F0FFF;
        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, bool bInheritHandle, int dwProcessId);
        [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
        private static extern byte WriteProcessMemoryByte(int Handle, int Address, ref byte Value, int Size, ref int BytesWritten);
        [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
        private static extern int WriteProcessMemoryInteger(int Handle, int Address, ref int Value, int Size, ref int BytesWritten);
        [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
        private static extern float WriteProcessMemoryFloat(int Handle, int Address, ref float Value, int Size, ref int BytesWritten);
        [DllImport("kernel32", EntryPoint = "WriteProcessMemory")]
        private static extern double WriteProcessMemoryDouble(int Handle, int Address, ref double Value, int Size, ref int BytesWritten);
        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress,
  byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern byte ReadProcessMemoryByte(int Handle, int Address, ref byte Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern int ReadProcessMemoryInteger(int Handle, int Address, ref int Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern float ReadProcessMemoryFloat(int Handle, int Address, ref float Value, int Size, ref int BytesRead);
        [DllImport("kernel32", EntryPoint = "ReadProcessMemory")]
        private static extern double ReadProcessMemoryDouble(int Handle, int Address, ref double Value, int Size, ref int BytesRead);
        [DllImport("kernel32")]
        private static extern int CloseHandle(int Handle);

        [DllImport("user32")]
        private static extern int FindWindow(string sClassName, string sAppName);
        [DllImport("user32")]
        private static extern int GetWindowThreadProcessId(int HWND, out int processId);


        [Flags]
        public enum ThreadAccess : int
        {
            TERMINATE = (0x0001),
            SUSPEND_RESUME = (0x0002),
            GET_CONTEXT = (0x0008),
            SET_CONTEXT = (0x0010),
            SET_INFORMATION = (0x0020),
            QUERY_INFORMATION = (0x0040),
            SET_THREAD_TOKEN = (0x0080),
            IMPERSONATE = (0x0100),
            DIRECT_IMPERSONATION = (0x0200)
        }
        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenThread(ThreadAccess dwDesiredAccess, bool bInheritHandle, uint dwThreadId);
        [DllImport("kernel32.dll")]
        static extern uint SuspendThread(IntPtr hThread);
        [DllImport("kernel32.dll")]
        static extern int ResumeThread(IntPtr hThread);

        private void SuspendProcess(int PID)
        {
            Process proc = Process.GetProcessById(PID);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                SuspendThread(pOpenThread);
            }
        }
        public void ResumeProcess(int PID)
        {
            Process proc = Process.GetProcessById(PID);

            if (proc.ProcessName == string.Empty)
                return;

            foreach (ProcessThread pT in proc.Threads)
            {
                IntPtr pOpenThread = OpenThread(ThreadAccess.SUSPEND_RESUME, false, (uint)pT.Id);

                if (pOpenThread == IntPtr.Zero)
                {
                    break;
                }

                ResumeThread(pOpenThread);
            }
        }

        #endregion
        [Flags]
        public enum AllocationType
        {
            Commit = 0x00001000,
            Reserve = 0x00002000,
            Decommit = 0x00004000,
            Release = 0x00008000,
            Reset = 0x00080000,
            TopDown = 0x00100000,
            WriteWatch = 0x00200000,
            Physical = 0x00400000,
            LargePages = 0x20000000
        }

        [Flags]
        public enum MemoryProtection
        {
            NoAccess = 0x0001,
            ReadOnly = 0x0002,
            ReadWrite = 0x0004,
            WriteCopy = 0x0008,
            Execute = 0x0010,
            ExecuteRead = 0x0020,
            ExecuteReadWrite = 0x0040,
            ExecuteWriteCopy = 0x0080,
            GuardModifierflag = 0x0100,
            NoCacheModifierflag = 0x0200,
            WriteCombineModifierflag = 0x0400
        }
        [DllImport("kernel32.dll")]
        private static extern uint VirtualAllocEx(IntPtr hProcess, uint dwAddress, int nSize, uint dwAllocationType, uint dwProtect);
        [DllImport("kernel32")]
        private static extern bool VirtualFreeEx(IntPtr hProcess, uint dwAddress, int nSize, uint dwFreeType);
        public Camera()
        {
            InitializeComponent();
        }
        IntPtr GTA;
        uint Address;
        uint dwBaseAddress;
        Process[] list;
        public static Fasm.ManagedFasm Assembler = new ManagedFasm();
        public void search(object threadcontext)
        {
            while (true)
            {
                list = Process.GetProcessesByName("gta2");
                if (list.Length != 0)
                {
                    Console.Beep();
                    break;
                }
                System.Threading.Thread.Sleep(100);
            }
            this.Invoke((MethodInvoker)delegate
            {
                GTA = OpenProcess(ProcessAccessFlags.All, false, list[0].Id);
                this.Text = "Found GTA2.exe";
                DoStuff();
            });
        }
        private void DoStuff()
        {
            Address = (uint)Aobscan.AobScan(GTA, new Byte[] { 0xE8, 0x2D, 0x37, 0xFE, 0xFF, 0x8B, 0x08, 0x8B, 0x54, 0x24, 0x14});
            if (Address == 0)
            {
                MessageBox.Show("Couldn't find adress to jump from.");
                Environment.Exit(0);
            }
            Address += 0xB;
            dwBaseAddress = VirtualAllocEx(GTA, 0, 0x300, 0x1000, 0x0040);
            Assembler = new ManagedFasm(GTA);
            Assembler.SetMemorySize(0x1000);
            Assembler.AddLine("push ebp");
            Assembler.AddLine("push ebx");
            Assembler.AddLine("push esi");
            Assembler.AddLine("cmp ecx,0FFFFC000h");
            Assembler.AddLine("je {0}", dwBaseAddress + Assembler.Assemble().Length + 0x50);
            Assembler.AddLine("mov dword ebp,{0}", dwBaseAddress + 0x200);
            Assembler.AddLine("mov dword ebx,[ebp]");
            Assembler.AddLine("cmp ebx,0");
            Assembler.AddLine("je {0}", dwBaseAddress + Assembler.Assemble().Length + 0x3);
            Assembler.AddLine("mov dword ecx,[ebp]");
            Assembler.AddLine("jmp {0}", dwBaseAddress + Assembler.Assemble().Length + 0x30);
            Assembler.AddLine("mov dword [ebp - 44h],ecx");
            //float x = 1f;
            //Assembler.AddLine("mov dword [ebp - 40h],[{0}]", dwBaseAddress+0x4E);
            Assembler.AddLine("fld dword [ebp-40h]");
            Assembler.AddLine("fild dword [ebp-44h] ");
            Assembler.AddLine("fstp dword [ebp - 0C8h]");
            Assembler.AddLine("fld dword [ebp - 0C8h]");
            Assembler.AddLine("fmulp");
            Assembler.AddLine("fstp qword [ebp - 0D0h]");
            Assembler.AddLine("movsd xmm0,[ebp - 0D0h]");
            Assembler.AddLine("cvttsd2si ecx, xmm0");
            Assembler.AddLine("cmp ecx,50000h");
            Assembler.AddLine("jng {0}",dwBaseAddress + Assembler.Assemble().Length -0x4);
            Assembler.AddLine("mov dword ecx,50000h");
            Assembler.AddLine("mov dword [ebp - 0E0h], ecx");
            Assembler.AddLine("mov dword [esi+18h], ecx");
            Assembler.AddLine("mov dword [esi+1Ch],edx");
            Assembler.AddLine("pop esi");
            Assembler.AddLine("pop ecx");
            Assembler.AddLine("pop ebp");
            Assembler.AddLine("pop ebx");
            Assembler.AddLine("ret 0010h");
            Assembler.Inject(dwBaseAddress);
            //int test = 0;
            //WriteProcessMemory((int)GTA, (int)dwBaseAddress +0x200 - 0x40, BitConverter.GetBytes(x), BitConverter.GetBytes(x).Length, ref test);
            Assembler.Clear();
            Assembler.AddLine("jmp {0}", dwBaseAddress);
            Assembler.Inject(Address);
            Reader.Enabled = true;
            checkBox1_CheckedChanged(this, new EventArgs());
            textBox1_TextChanged(this, new EventArgs());
            numericUpDown1_ValueChanged(this, new EventArgs());
            //MessageBox.Show("Allocated memory to 0x" + dwBaseAddress.ToString("X8") + Environment.NewLine + "jumped to from 0x" + Address.ToString("X8"));
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback(search));
        }

        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {
            int test = 0;
            WriteProcessMemory((int)GTA, (int)dwBaseAddress + 0x200 - 0x40, BitConverter.GetBytes((float)numericUpDown1.Value), BitConverter.GetBytes((float)numericUpDown1.Value).Length, ref test);
        }

        private void Reader_Tick(object sender, EventArgs e)
        {
            list = Process.GetProcessesByName("gta2");
            if (list.Length == 0)
            {
                Console.Beep();
                DIST.Text = "NIL";
                Reader.Enabled = false;
                this.Text= "Searching for GTA2.exe";
                 ThreadPool.QueueUserWorkItem(new WaitCallback(search));
                return;
            }
            int val = 0;
            int bytes = 0;
            ReadProcessMemoryInteger((int)GTA, (int)dwBaseAddress + 0x200 - 0xE0, ref val, 4, ref bytes);
            DIST.Text = "0x"+ val.ToString("X");
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            Assembler.Clear();
            if (checkBox1.Checked)
            {
                Assembler.AddLine("jmp {0}", dwBaseAddress + 0x53);
            }
            else
            {
                Assembler.AddLine("jng {0}", dwBaseAddress + 0x53);
            }

            Assembler.Inject(dwBaseAddress + 0x4C);
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            int hex = 0;
            int test = 0;
            if (int.TryParse(textBox1.Text, System.Globalization.NumberStyles.HexNumber, null, out hex))
            {
                WriteProcessMemory((int)GTA, (int)dwBaseAddress + 0x200, BitConverter.GetBytes(hex), BitConverter.GetBytes(hex).Length, ref test);
                frozen.Text = "0x" + hex.ToString("X");
            }
        }
    }
}
