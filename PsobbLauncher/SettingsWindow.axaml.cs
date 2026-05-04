using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace PsobbLauncher
{
    public partial class SettingsWindow : Window
    {
        private const string RegPath = @"Software\SonicTeam\PSOBB";

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                InitializeGamepad();
            }
        }

        private string GetGameDirectory()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string root = currentDir;
            if (!File.Exists(Path.Combine(root, "psobb.exe")) && File.Exists(Path.Combine(root, "..", "psobb.exe")))
                root = Path.GetFullPath(Path.Combine(root, ".."));
            return root;
        }

        private void LoadSettings()
        {
            try
            {
                ComboResolution.SelectedIndex = 3; 
                ComboMode.SelectedIndex = 1; 
                
                CheckAdvEffect.IsChecked = true;
                ComboShadow.SelectedIndex = 2; 
                ComboMap.SelectedIndex = 2;    
                ComboClip.SelectedIndex = 2;   
                ComboFog.SelectedIndex = 2;    
                CheckHighResTex.IsChecked = true;
                ComboHUDScale.SelectedIndex = 0; 
                ComboFrameSkip.SelectedIndex = 0; 

                CheckSoundGlobal.IsChecked = true;
                CheckBGM.IsChecked = true;
                CheckSE.IsChecked = true;

                string root = GetGameDirectory();
                string widescreenCfg = Path.Combine(root, "widescreen.cfg");
                string framegenCfg = Path.Combine(root, "framegen.cfg");

                if (File.Exists(widescreenCfg))
                {
                    var lines = File.ReadAllLines(widescreenCfg);
                    int width = 0, height = 0, windowed = 1; 

                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string val = parts[1].Trim();
                            if (key.Equals("Width", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out width);
                            if (key.Equals("Height", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out height);
                            if (key.Equals("Windowed", StringComparison.OrdinalIgnoreCase)) int.TryParse(val, out windowed);
                            if (key.Equals("DebugLogsEnabled", StringComparison.OrdinalIgnoreCase))
                            {
                                int.TryParse(val, out int dbg);
                                CheckDebugLogs.IsChecked = (dbg != 0);
                            }
                            if (key.Equals("HUDScale", StringComparison.OrdinalIgnoreCase))
                            {
                                foreach (ComboBoxItem item in ComboHUDScale.Items)
                                {
                                     if (item.Tag != null && item.Tag.ToString() == val)
                                     {
                                         ComboHUDScale.SelectedItem = item;
                                         break;    
                                     }
                                }
                            }
                        }
                    }

                    ComboMode.SelectedIndex = (windowed != 0) ? 1 : 0;

                    if (width > 0 && height > 0)
                    {
                        string target = $"{width} x {height}";
                        foreach (ComboBoxItem item in ComboResolution.Items)
                        {
                            if (item.Content?.ToString() == target)
                            {
                                ComboResolution.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                if (File.Exists(framegenCfg))
                {
                    var lines = File.ReadAllLines(framegenCfg);
                    foreach (var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            string key = parts[0].Trim();
                            string val = parts[1].Trim();
                            
                            if (key.Equals("FrameGen", StringComparison.OrdinalIgnoreCase))
                            {
                                CheckFrameGen.IsChecked = (val == "1");
                                GridTargetHz.IsVisible = (val == "1");
                            }
                            if (key.Equals("TargetHz", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(val, out int targetHz))
                                {
                                    foreach (ComboBoxItem item in ComboTargetHz.Items)
                                    {
                                        if (item.Tag != null && item.Tag.ToString() == val)
                                        {
                                            ComboTargetHz.SelectedItem = item;
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(RegPath))
                    {
                        if (key != null)
                        {
                             object gfxObj = key.GetValue("GRAPHICCTRL");
                             if (gfxObj is byte[] gfxBytes && gfxBytes.Length >= 36)
                             {
                                  int adv = BitConverter.ToInt32(gfxBytes, 4);
                                  CheckAdvEffect.IsChecked = adv != 0;

                                 int shadow = BitConverter.ToInt32(gfxBytes, 8);
                                 ComboShadow.SelectedIndex = Math.Min(2, Math.Max(0, shadow));

                                 int map = BitConverter.ToInt32(gfxBytes, 16);
                                 ComboMap.SelectedIndex = Math.Min(2, Math.Max(0, map));

                                 int clip = BitConverter.ToInt32(gfxBytes, 20);
                                 ComboClip.SelectedIndex = Math.Min(2, Math.Max(0, clip));

                                 int fog = BitConverter.ToInt32(gfxBytes, 24);
                                 ComboFog.SelectedIndex = Math.Min(2, Math.Max(0, fog));

                                 int lowTex = BitConverter.ToInt32(gfxBytes, 28);
                                 CheckHighResTex.IsChecked = lowTex == 0;

                                 int skip = BitConverter.ToInt32(gfxBytes, 32);
                                 if (skip < 0 || skip > 2) ComboFrameSkip.SelectedIndex = 0;
                                 else ComboFrameSkip.SelectedIndex = skip;
                             }

                            object vsync = key.GetValue("VSync");
                            if (vsync != null) CheckVsync.IsChecked = ((int)vsync) != 0;

                            object sndObj = key.GetValue("SOUNDCTRL");
                            if (sndObj is byte[] sndBytes && sndBytes.Length >= 12)
                            {
                                int setup = BitConverter.ToInt32(sndBytes, 0);
                                int bgm = BitConverter.ToInt32(sndBytes, 4);
                                int se = BitConverter.ToInt32(sndBytes, 8);

                                CheckSoundGlobal.IsChecked = setup != 0;
                                CheckBGM.IsChecked = bgm != 0;
                                CheckSE.IsChecked = se != 0;
                            }

                            object saveLogin = key.GetValue("ACCOUNT_CHECK");
                            if (saveLogin != null) CheckSaveLogin.IsChecked = ((int)saveLogin) != 0;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error loading settings: " + ex.Message);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string root = GetGameDirectory();
                string widescreenCfg = Path.Combine(root, "widescreen.cfg");
                string framegenCfg = Path.Combine(root, "framegen.cfg");

                int width = 0;
                int height = 0;
                if (ComboResolution.SelectedItem is ComboBoxItem item)
                {
                    string contentStr = item.Content?.ToString() ?? "";
                    string[] parts = contentStr.Split(new string[] { " x " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out width);
                        int.TryParse(parts[1], out height);
                    }
                }

                int windowed = 1; 
                if (ComboMode.SelectedIndex == 0) windowed = 0;
                else if (ComboMode.SelectedIndex == 1) windowed = 1;
                else if (ComboMode.SelectedIndex == 2) windowed = 2; 

                StringBuilder sbWide = new StringBuilder();
                sbWide.AppendLine($"Windowed={windowed}");
                if (width > 0 && height > 0)
                {
                    sbWide.AppendLine($"Width={width}");
                    sbWide.AppendLine($"Height={height}");
                }
                sbWide.AppendLine("MSAA=1");
                sbWide.AppendLine("SMAA=1");
                sbWide.AppendLine("SSAO=1");
                sbWide.AppendLine("CelShader=1");
                sbWide.AppendLine("DOF=1");
                sbWide.AppendLine("HDR=1");
                
                string hudScale = "1.0";
                if (ComboHUDScale.SelectedItem is ComboBoxItem hudItem && hudItem.Tag != null)
                     hudScale = hudItem.Tag.ToString() ?? "1.0";
                sbWide.AppendLine($"HUDScale={hudScale}");

                sbWide.AppendLine($"DebugLogsEnabled={(CheckDebugLogs.IsChecked == true ? 1 : 0)}");

                File.WriteAllText(widescreenCfg, sbWide.ToString());

                int frameGen = (CheckFrameGen.IsChecked == true) ? 1 : 0;
                int targetHz = 60;
                if (ComboTargetHz.SelectedItem is ComboBoxItem hzItem && hzItem.Tag != null) {
                    int.TryParse(hzItem.Tag.ToString(), out targetHz);
                }

                StringBuilder sbGen = new StringBuilder();
                sbGen.AppendLine("[General]");
                sbGen.AppendLine($"FrameGen={frameGen}");
                sbGen.AppendLine($"TargetHz={targetHz}");

                File.WriteAllText(framegenCfg, sbGen.ToString());

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(RegPath))
                    {
                        if (key != null)
                        {
                            key.SetValue("VSync", CheckVsync.IsChecked == true ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                            key.SetValue("BitDepth", 32, Microsoft.Win32.RegistryValueKind.DWord);

                            int p0_preset   = 3; 
                            int p1_adv      = CheckAdvEffect.IsChecked == true ? 1 : 0;
                            int p2_shadow   = ComboShadow.SelectedIndex; if (p2_shadow < 0) p2_shadow = 2;
                            int p3_enemy    = 1; 
                            int p4_map      = ComboMap.SelectedIndex; if (p4_map < 0) p4_map = 2;
                            int p5_clip     = ComboClip.SelectedIndex; if (p5_clip < 0) p5_clip = 2;
                            int p6_fog      = ComboFog.SelectedIndex; if (p6_fog < 0) p6_fog = 2;
                            int p7_lowTex   = CheckHighResTex.IsChecked == true ? 0 : 1; 
                            int p8_skip     = ComboFrameSkip.SelectedIndex; if (p8_skip < 0) p8_skip = 0;

                            int[] graphicCtrlInts = new int[] { p0_preset, p1_adv, p2_shadow, p3_enemy, p4_map, p5_clip, p6_fog, p7_lowTex, p8_skip };
                            byte[] graphicCtrlBytes = new byte[36];
                            for (int i = 0; i < 9; i++)
                            {
                                byte[] b = BitConverter.GetBytes(graphicCtrlInts[i]);
                                Array.Copy(b, 0, graphicCtrlBytes, i * 4, 4);
                            }
                            key.SetValue("GRAPHICCTRL", graphicCtrlBytes, Microsoft.Win32.RegistryValueKind.Binary);

                            int soundSetup = CheckSoundGlobal.IsChecked == true ? 1 : 0;
                            int soundBgm = CheckBGM.IsChecked == true ? 1 : 0;
                            int soundSe = CheckSE.IsChecked == true ? 1 : 0;
                            int[] soundCtrlInts = new int[] { soundSetup, soundBgm, soundSe };
                            byte[] soundCtrlBytes = new byte[12];
                            for (int i = 0; i < 3; i++)
                            {
                                byte[] b = BitConverter.GetBytes(soundCtrlInts[i]);
                                Array.Copy(b, 0, soundCtrlBytes, i * 4, 4);
                            }
                            key.SetValue("SOUNDCTRL", soundCtrlBytes, Microsoft.Win32.RegistryValueKind.Binary);

                            key.SetValue("ACCOUNT_CHECK", CheckSaveLogin.IsChecked == true ? 1 : 0, Microsoft.Win32.RegistryValueKind.DWord);
                        }
                    }
                }
                
                ApplyGraphicsFramework();

                this.Close(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error saving: " + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close(false);
        }

        private void TitleBar_PointerPressed(object sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) this.BeginMoveDrag(e);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void SafeDelete(string path)
        {
             if (File.Exists(path)) try { File.Delete(path); } catch { }
        }

        private void ApplyGraphicsFramework()
        {
            try
            {
                string root = GetGameDirectory();
                string frameworksDir = Path.Combine(root, "Frameworks");
                
                string destFile = Path.Combine(root, "d3d8.dll");
                if (!File.Exists(destFile))
                {
                    string d3d8Wrapper = Path.Combine(frameworksDir, "d3d8to9.dll"); 
                    if (File.Exists(d3d8Wrapper)) 
                    {
                        File.Copy(d3d8Wrapper, destFile, true);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Framework apply error: " + ex.Message);
            }
        }

        private void CheckFrameGen_Checked(object sender, RoutedEventArgs e)
        {
            if (GridTargetHz != null) GridTargetHz.IsVisible = CheckFrameGen.IsChecked == true;
        }
        
        // ---------------------------------------------------------
        // Gamepad Support (Simple XInput Poll - Windows Only)
        // ---------------------------------------------------------
        private DispatcherTimer _gamepadTimer;
        private DateTime _lastInputTime = DateTime.MinValue;
        private const int INPUT_DEBOUNCE_MS = 150; 

        private void InitializeGamepad()
        {
            _gamepadTimer = new DispatcherTimer();
            _gamepadTimer.Interval = TimeSpan.FromMilliseconds(33); 
            _gamepadTimer.Tick += GamepadTimer_Tick;
            _gamepadTimer.Start();
        }

        private void GamepadTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                XInputState state = new XInputState();
                if (XInputNative.XInputGetState(0, ref state) == 0) 
                {
                    ProcessGamepadInput(state.Gamepad);
                }
            }
            catch 
            {
                // Ignore if xinput fails
            }
        }

        private void ProcessGamepadInput(XInputGamepad pad)
        {
            if ((DateTime.Now - _lastInputTime).TotalMilliseconds < INPUT_DEBOUNCE_MS)
                return;

            bool inputProcessed = false;
            
            short deadzone = 8000;
            bool up = (pad.wButtons & 0x0001) != 0 || pad.sThumbLY > deadzone;    
            bool down = (pad.wButtons & 0x0002) != 0 || pad.sThumbLY < -deadzone; 
            bool left = (pad.wButtons & 0x0004) != 0 || pad.sThumbLX < -deadzone; 
            bool right = (pad.wButtons & 0x0008) != 0 || pad.sThumbLX > deadzone; 
            bool aBtn = (pad.wButtons & 0x1000) != 0; 
            bool bBtn = (pad.wButtons & 0x2000) != 0; 

            if (up)
            {
                MoveFocus(NavigationDirection.Up);
                inputProcessed = true;
            }
            else if (down)
            {
                MoveFocus(NavigationDirection.Down);
                inputProcessed = true;
            }
            else if (left)
            {
                 MoveFocus(NavigationDirection.Left);
                 inputProcessed = true;
            }
            else if (right)
            {
                 MoveFocus(NavigationDirection.Right);
                 inputProcessed = true;
            }
            else if (aBtn)
            {
                 var element = TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() as Control;
                 if (element != null)
                 {
                     if (element is ComboBox cb && !cb.IsDropDownOpen)
                     {
                         cb.IsDropDownOpen = true;
                     }
                     else if (element is Button btn)
                     {
                         btn.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                     }
                     else if (element is CheckBox chk)
                     {
                         chk.IsChecked = !chk.IsChecked;
                     }
                 }
                 inputProcessed = true;
            }

            if (inputProcessed)
                _lastInputTime = DateTime.Now;
        }

        /// <summary>
        /// Placeholder for gamepad-driven focus navigation.
        /// Avalonia 11 does not expose a simple programmatic focus-move API like WPF,
        /// so this currently relies on the framework's built-in keyboard navigation.
        /// </summary>
        private void MoveFocus(NavigationDirection direction)
        {
            // Intentional no-op — Avalonia's default keyboard navigation handles this.
        }
    }

    internal static class XInputNative
    {
        [DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        public static extern int XInputGetState(int dwUserIndex, ref XInputState pState);
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XInputState
    {
        public int PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct XInputGamepad
    {
        public short wButtons;
        public byte bLeftTrigger;
        public byte bRightTrigger;
        public short sThumbLX;
        public short sThumbLY;
        public short sThumbRX;
        public short sThumbRY;
    }
}
