using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using Gma.UserActivityMonitor;
using Prism.Commands;
using Prism.Mvvm;

namespace KeyBoardApp.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        // ... { GLOBAL HOOK }
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("user32.dll")]
        static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll")]
        static extern short GetKeyState(int nCode);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("user32.dll")]
        static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);


        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13;
        const int WM_KEYDOWN = 0x1;
        const int WM_KEYUP = 0x2;

        const uint MOUSEEVENTF_LEFTDOWN = 0x0002;      // The left button is down.
        const uint MOUSEEVENTF_LEFTUP = 0x0004;        // The left button is up.

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        DateTime CaptureStartTime;
        
        public List<(KeyDto keyDto, TimeSpan keyTime)> KeyHist { get; set; }
        public List<(MouseDto mouseDto, TimeSpan mouseTime)> MouseHist { get; set; }
        public Dictionary<string, KeyDto> MyKeyStatusList { get; set; }


        #region [Prop] IsCapture
        private bool isCapture;
        public bool IsCapture
        {
            get { return isCapture; }
            set { SetProperty(ref isCapture, value); }
        }
        #endregion

        #region [Prop] IsPlay
        private bool isPlay;
        public bool IsPlay
        {
            get { return isPlay; }
            set { SetProperty(ref isPlay, value); }
        } 
        #endregion

        #region [Prop] InputKey
        private string inputKey;
        public string InputKey
        {
            get { return inputKey; }
            set { SetProperty(ref inputKey, value); }
        }
        #endregion

        #region [Prop] Opacity
        private double opacity;
        public double Opacity
        {
            get { return opacity; }
            set 
            {
                Properties.Settings.Default.Opacity = value;
                Properties.Settings.Default.Save();
                SetProperty(ref opacity, value); 
            }
        }
        #endregion

        #region [Prop] IsShowDisp
        private bool isShowDisp;
        public bool IsShowDisp
        {
            get { return isShowDisp; }
            set { SetProperty(ref isShowDisp, value); }
        } 
        #endregion

        #region [Create] MainWindowViewModel
        public MainWindowViewModel()
        {
            MouseHist = new List<(MouseDto mouseDto, TimeSpan mouseTime)>();
            KeyHist = new List<(KeyDto, TimeSpan)>();
            var keyList = Enum.GetValues(typeof(VKeys)).Cast<VKeys>().ToList();
            Opacity = Properties.Settings.Default.Opacity;
            MyKeyStatusList = keyList.ToDictionary(k => k.ToString(), v => new KeyDto { KeyName = v.ToString().Replace("VK_", ""), Key = v });
        } 
        #endregion

        #region [Cmd] LoadedCommand
        private DelegateCommand loadedCommand;
        public DelegateCommand LoadedCommand =>
            loadedCommand ?? (loadedCommand = new DelegateCommand(ExecuteLoadedCommand));

        void ExecuteLoadedCommand()
        {
            SetHook();
            //HookManager.MouseUp_Block = false;
            //HookManager.MouseDown_Block = false;

            var isMouseDown = false;
            HookManager.MouseMove += (s, e) => 
            {
                //if (isMouseDown)
                    MouseHistAdd(VMouse.VM_MOUSE_MOVE, e); 
            };
            HookManager.MouseUp += (s, e) => 
            {
                isMouseDown = false;
                MouseHistAdd(VMouse.VM_MOUSE_UP, e); 
            };
            HookManager.MouseDown += (s, e) => 
            {
                isMouseDown = true;
                MouseHistAdd(VMouse.VM_MOUSE_DOWN, e); 
            };
        }

        private void MouseHistAdd(VMouse mouseEventType, System.Windows.Forms.MouseEventArgs e)
        {
            if (IsCapture)
                MouseHist.Add((new MouseDto { MouseType = mouseEventType, MousePoint = e.Location }, DateTime.Now - CaptureStartTime));
        }
        #endregion

        #region [Cmd] UnloadedCommand
        private DelegateCommand unloadedCommand;
        public DelegateCommand UnloadedCommand =>
            unloadedCommand ?? (unloadedCommand = new DelegateCommand(ExecuteUnloadedCommand));

        void ExecuteUnloadedCommand()
        {
            UnHook();
        }
        #endregion

        #region [Method] SetHook
        public void SetHook()
        {
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
            
        }
        #endregion

        #region [Method] UnHook
        public static void UnHook()
        {
            UnhookWindowsHookEx(hhook);
        }
        #endregion

        #region [Method] hookProc
        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam);
            var vKey = ((VKeys)vkCode).ToString();
            var mainVM = System.Windows.Application.Current.MainWindow.DataContext as MainWindowViewModel;
            var isPress = (int)wParam == 257 || (int)wParam == 261 ? false : true;

            VKeys myKey = (VKeys)vkCode;
            var LCtrl = GetKeyState((int)VKeys.VK_LCONTROL) & 0x80;
            var LShift = GetKeyState((int)VKeys.VK_LSHIFT) & 0x80;
            var F1 = GetKeyState((int)VKeys.VK_F1) & 0x80;
            if (LCtrl != 0)
                myKey |= VKeys.VK_LCONTROL;
            if (LShift != 0)
                myKey |= VKeys.VK_LSHIFT;
            if (F1 != 0)
                myKey |= VKeys.VK_F1;

            if ((int)myKey == ((int)VKeys.VK_LCONTROL | (int)VKeys.VK_LSHIFT | (int)VKeys.VK_F1))
            {
                if (!isPress)
                    mainVM.CmdCapturePlay.Execute();

                return (IntPtr)1;
            }
            else
            {
                var dic = mainVM.MyKeyStatusList;

                if (dic.ContainsKey(vKey))
                {
                    dic[vKey].IsPress = (int)wParam == 257 || (int)wParam == 261 ? false : true;
                    //Console.WriteLine($"{code}, {(int)wParam}, {lParam}, {vKey}, {dic[vKey].IsPress}");
                    if (mainVM.isCapture)
                        mainVM.KeyHist.Add((new KeyDto { KeyName = vKey, IsPress = dic[vKey].IsPress, Key = dic[vKey].Key }, DateTime.Now - mainVM.CaptureStartTime));
                }
                return CallNextHookEx(hhook, code, (int)wParam, lParam);
            }
        } 
        #endregion

        #region [Cmd] CmdCaptureStart
        private DelegateCommand cmdCaptureStart;
        public DelegateCommand CmdCaptureStart =>
            cmdCaptureStart ?? (cmdCaptureStart = new DelegateCommand(ExecuteCmdCaptureStart));

        void ExecuteCmdCaptureStart()
        {
            KeyHist.Clear();
            MouseHist.Clear();
            CaptureStartTime = DateTime.Now;
            IsCapture = true;
        }
        #endregion

        #region [Cmd] CmdCaptureStop
        private DelegateCommand cmdCaptureStop;
        public DelegateCommand CmdCaptureStop =>
            cmdCaptureStop ?? (cmdCaptureStop = new DelegateCommand(ExecuteCmdCaptureStop));

        void ExecuteCmdCaptureStop()
        {
            IsCapture = false;
        }
        #endregion

        #region [Cmd] CmdCapturePlay
        private DelegateCommand cmdCapturePlay;
        public DelegateCommand CmdCapturePlay =>
            cmdCapturePlay ?? (cmdCapturePlay = new DelegateCommand(ExecuteCmdCapturePlay));

        async void ExecuteCmdCapturePlay()
        {
            IsPlay = !IsPlay;

            if (IsPlay)
            {
                var beforeSpan = new TimeSpan();
                var allCaptureList = MouseHist.Select(g => new EventOrderDto { EventType = EventOrder.MOUSE, MouseEvt = g }).ToList();
                allCaptureList.AddRange(KeyHist.Select(g => new EventOrderDto { EventType = EventOrder.KEYBOARD, KeyEvt = g }));

                allCaptureList = allCaptureList.OrderBy(g => g.MouseEvt.mouseTime + g.KeyEvt.keyTime).ToList();
                
                foreach (var item in allCaptureList)
                {
                    if (!IsPlay) break;

                    switch (item.EventType)
                    {
                        case EventOrder.KEYBOARD:
                            var keyDelay = Convert.ToInt32((item.KeyEvt.keyTime - beforeSpan).TotalMilliseconds);
                            await Task.Delay(keyDelay);
                            beforeSpan = item.KeyEvt.keyTime;
                            MyKeyStatusList[item.KeyEvt.keyDto.KeyName].IsPress = item.KeyEvt.keyDto.IsPress;
                            keybd_event(byte.Parse(((int)item.KeyEvt.keyDto.Key).ToString()), 0x45, (uint)(item.KeyEvt.keyDto.IsPress ? WM_KEYDOWN : WM_KEYUP), UIntPtr.Zero);

                            Console.WriteLine($"{DateTime.Now} : 키보드 온");
                            break;
                        case EventOrder.MOUSE:
                            var MouseDelay = Convert.ToInt32((item.MouseEvt.mouseTime - beforeSpan).TotalMilliseconds);
                            await Task.Delay(MouseDelay);
                            beforeSpan = item.MouseEvt.mouseTime;

                            System.Windows.Forms.Cursor.Position = item.MouseEvt.mouseDto.MousePoint;
                            switch (item.MouseEvt.mouseDto.MouseType)
                            {
                                case VMouse.VM_MOUSE_UP:
                                    mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    break;
                                case VMouse.VM_MOUSE_DOWN:

                                    mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    break;
                                //case VMouse.VM_MOUSE_MOVE:
                                //    System.Windows.Forms.Cursor.Position = item.MouseEvt.mouseDto.MousePoint;
                                    break;
                                default:
                                    break;
                            }
                            Console.WriteLine($"{DateTime.Now} : 마우스 온");

                            break;
                        default:
                            break;
                    }                   
                }
            }

            IsPlay = false;
            //return;
            //if (IsPlay)
            //{
            //    while (IsPlay)
            //    {
            //        var beforeSpan = new TimeSpan();
            //        foreach (var item in KeyHist)
            //        {
            //            if (!IsPlay) break;

            //            var delay = Convert.ToInt32((item.keyTime - beforeSpan).TotalMilliseconds);
            //            await Task.Delay(delay);
            //            MyKeyStatusList[item.keyDto.KeyName].IsPress = item.keyDto.IsPress;
            //            keybd_event(byte.Parse(((int)item.keyDto.Key).ToString()), 0x45, (uint)(item.keyDto.IsPress ? WM_KEYDOWN : WM_KEYUP), UIntPtr.Zero);
            //            beforeSpan = item.keyTime;
            //        }
            //        Console.WriteLine("아직 온");
            //        await Task.Delay(500);
            //    }
            //}
        }
        #endregion

        #region [Cmd] CmdExit
        private DelegateCommand cmdExit;
        public DelegateCommand CmdExit =>
            cmdExit ?? (cmdExit = new DelegateCommand(ExecuteCmdExit));

        void ExecuteCmdExit()
        {
            Environment.Exit(0);
        }
        #endregion

        #region [Cmd] CmdExitMouseOver
        private DelegateCommand<object> cmdExitMouseOver;
        public DelegateCommand<object> CmdExitMouseOver =>
            cmdExitMouseOver ?? (cmdExitMouseOver = new DelegateCommand<object>(ExecuteCmdExitMouseOver));

        void ExecuteCmdExitMouseOver(object flag)
        {
            var bln = Boolean.Parse(flag.ToString());
            IsShowDisp = bln;
        } 
        #endregion
    }

    
}
