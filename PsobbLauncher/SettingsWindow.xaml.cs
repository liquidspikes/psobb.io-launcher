using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using Microsoft.Win32;

namespace PsobbLauncher
{
    public partial class SettingsWindow : Window
    {
        private const string RegPath = @"Software\SonicTeam\PSOBB";

        public SettingsWindow()
        {
            InitializeComponent();
            LoadSettings();
            InitializeGamepad();
        }

        private string GetGameDirectory()
        {
            string currentDir = AppDomain.CurrentDomain.BaseDirectory;
            string root = currentDir;
            // Check parent specifically to find psobb.exe if we are in a subdir
            if (!File.Exists(Path.Combine(root, "psobb.exe")) && File.Exists(Path.Combine(root, "..", "psobb.exe")))
                root = Path.GetFullPath(Path.Combine(root, ".."));
            return root;
        }

        private void LoadSettings()
        {
            try
            {
                // Defaults
                ComboResolution.SelectedIndex = 3; // 1280x720
                ComboMode.SelectedIndex = 1; // Windowed default
                
                // Graphics Defaults (High/Far/NoSkip)
                CheckAdvEffect.IsChecked = true;
                ComboShadow.SelectedIndex = 2; // High
                ComboMap.SelectedIndex = 2;    // Far
                ComboClip.SelectedIndex = 2;   // Far
                ComboFog.SelectedIndex = 2;    // Far
                CheckHighResTex.IsChecked = true;
                ComboHUDScale.SelectedIndex = 0; // Auto
                ComboFrameSkip.SelectedIndex = 0; // No Skip

                // Sound Defaults
                CheckSoundGlobal.IsChecked = true;
                CheckBGM.IsChecked = true;
                CheckSE.IsChecked = true;

                string root = GetGameDirectory();
                string widescreenCfg = Path.Combine(root, "widescreen.cfg");
                string framegenCfg = Path.Combine(root, "framegen.cfg");

                // --- Load widescreen.cfg ---
                // Format: Key=Value
                if (File.Exists(widescreenCfg))
                {
                    var lines = File.ReadAllLines(widescreenCfg);
                    int width = 0, height = 0, windowed = 1; // Default windowed 1 so we fall into windowed ui logic

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
                                // Match to Tag
                                foreach (System.Windows.Controls.ComboBoxItem item in ComboHUDScale.Items)
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

                    // Apply Windowed Mode
                    // 0 = Fullscreen, 1 = Windowed, 2 = Virtual Fullscreen?
                    // Logic was: windowed != 0 -> Windowed. 
                    // New Combo: 0=FS, 1=Windowed, 2=Virtual
                    // Let's map standard logic: 0 -> 0 (FS), anything else -> 1 (Windowed).
                    // If user manually set 2 in config? Treat as windowed for now unless we support it fully.
                    // For now, simple mapping:
                    ComboMode.SelectedIndex = (windowed != 0) ? 1 : 0;

                    // Apply Resolution ...
                    // ... (Resolution logic)

                    // Re-read file to get debug logs? Or just add variable above.
                    // Let's rewrite the block cleanly.


                    // Apply Resolution
                    if (width > 0 && height > 0)
                    {
                        string target = $"{width} x {height}";
                        foreach (System.Windows.Controls.ComboBoxItem item in ComboResolution.Items)
                        {
                            if (item.Content.ToString() == target)
                            {
                                ComboResolution.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }

                // --- Load framegen.cfg ---
                // Simple INI-like structure
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
                                GridTargetHz.Visibility = (val == "1") ? Visibility.Visible : Visibility.Collapsed;
                            }
                            if (key.Equals("TargetHz", StringComparison.OrdinalIgnoreCase))
                            {
                                if (int.TryParse(val, out int targetHz))
                                {
                                    // Map target Hz to item by Tag
                                    foreach (System.Windows.Controls.ComboBoxItem item in ComboTargetHz.Items)
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

                // --- Load Registry (Legacy / Other Settings) ---
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(RegPath))
                {
                    if (key != null)
                    {
                        // GraphicCtrl (We read partial mostly for UI state if we want to persist detailed settings not in cfg)
                        // However, plan asks for STATIC defaults in widescreen.cfg for most things.
                        // So we only really care about things NOT in widescreen.cfg if any.
                        // Actually, for consistency let's just stick to UI defaults for advanced stuff 
                        // as we overwrite them with static values anyway?
                        // "widescreen.cfg which contains... MSAA=1... etc" - implying these are FORCED writes.
                        
                        // We do check GraphicCtrl for things like Shadow, Map, etc?
                        // Implementation plan says: "widescreen.cfg defaults... static defaults provided"
                        // But Launcher UI has Shadow/Map/Clip/Fog/FrameSkip options.
                        // If these are not in widescreen.cfg structure provided by user, where do they go?
                        // User provided specific list: MSAA, SMAA, SSAO, CelShader, DOF, HDR, HUDScale.
                        // Existing Registry `GRAPHICCTRL` covers Shadow, Map, Clip, Fog.
                        // User didn't ask to remove `GRAPHICCTRL`, just "doesnt store the resolution".
                        // And asked to write `widescreen.cfg` and `framegen.cfg`.
                        // I will maintain `GRAPHICCTRL` registry reading/writing for the options NOT covered by configs.

                         object gfxObj = key.GetValue("GRAPHICCTRL");
                         if (gfxObj is byte[] gfxBytes && gfxBytes.Length >= 36)
                         {
                              // 1: Advanced Effect (0/1)
                              int adv = BitConverter.ToInt32(gfxBytes, 4);
                              CheckAdvEffect.IsChecked = adv != 0;

                              // 2: Shows (0/1/2)
                             int shadow = BitConverter.ToInt32(gfxBytes, 8);
                             ComboShadow.SelectedIndex = Math.Min(2, Math.Max(0, shadow));

                             // 4: Map (0/1/2)
                             int map = BitConverter.ToInt32(gfxBytes, 16);
                             ComboMap.SelectedIndex = Math.Min(2, Math.Max(0, map));

                             // 5: Clip (0/1/2)
                             int clip = BitConverter.ToInt32(gfxBytes, 20);
                             ComboClip.SelectedIndex = Math.Min(2, Math.Max(0, clip));

                             // 6: Fog (0/1/2)
                             int fog = BitConverter.ToInt32(gfxBytes, 24);
                             ComboFog.SelectedIndex = Math.Min(2, Math.Max(0, fog));

                             // 7: LowResTex (0=High, 1=Low)
                             int lowTex = BitConverter.ToInt32(gfxBytes, 28);
                             CheckHighResTex.IsChecked = lowTex == 0;

                             // 8: Frame Skip
                             int skip = BitConverter.ToInt32(gfxBytes, 32);
                             if (skip < 0 || skip > 2) ComboFrameSkip.SelectedIndex = 0;
                             else ComboFrameSkip.SelectedIndex = skip;
                         }

                        // VSync
                        object vsync = key.GetValue("VSync");
                        if (vsync != null) CheckVsync.IsChecked = ((int)vsync) != 0;

                        // Sound
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

                        // Save Login
                        object saveLogin = key.GetValue("ACCOUNT_CHECK");
                        if (saveLogin != null) CheckSaveLogin.IsChecked = ((int)saveLogin) != 0;

                        // Debug Logs (Launcher specific?) -> Removed from Registry
                        // object debugObj = key.GetValue("DebugLogsEnabled");
                        // CheckDebugLogs.IsChecked = (debugObj != null && (int)debugObj != 0);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading settings: " + ex.Message);
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string root = GetGameDirectory();
                string widescreenCfg = Path.Combine(root, "widescreen.cfg");
                string framegenCfg = Path.Combine(root, "framegen.cfg");

                // --- Write widescreen.cfg ---
                int width = 0;
                int height = 0;
                if (ComboResolution.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    string[] parts = item.Content.ToString().Split(new string[] { " x " }, StringSplitOptions.None);
                    if (parts.Length == 2)
                    {
                        int.TryParse(parts[0], out width);
                        int.TryParse(parts[1], out height);
                    }
                }

                int windowed = 1; 
                // ComboMode: 0=Fullscreen, 1=Windowed, 2=Virtual
                if (ComboMode.SelectedIndex == 0) windowed = 0;
                else if (ComboMode.SelectedIndex == 1) windowed = 1;
                else if (ComboMode.SelectedIndex == 2) windowed = 2; // Support Virtual Fullscreen writing if selected

                StringBuilder sbWide = new StringBuilder();
                sbWide.AppendLine($"Windowed={windowed}");
                if (width > 0 && height > 0)
                {
                    sbWide.AppendLine($"Width={width}");
                    sbWide.AppendLine($"Height={height}");
                }
                // Static defaults as requested
                sbWide.AppendLine("MSAA=1");
                sbWide.AppendLine("SMAA=1");
                sbWide.AppendLine("SSAO=1");
                sbWide.AppendLine("CelShader=1");
                sbWide.AppendLine("DOF=1");
                sbWide.AppendLine("HDR=1");
                sbWide.AppendLine("DOF=1");
                sbWide.AppendLine("HDR=1");
                
                string hudScale = "1.0";
                if (ComboHUDScale.SelectedItem is System.Windows.Controls.ComboBoxItem hudItem && hudItem.Tag != null)
                     hudScale = hudItem.Tag.ToString();
                sbWide.AppendLine($"HUDScale={hudScale}");

                sbWide.AppendLine($"DebugLogsEnabled={(CheckDebugLogs.IsChecked == true ? 1 : 0)}");

                File.WriteAllText(widescreenCfg, sbWide.ToString());


                // --- Write framegen.cfg ---
                int frameGen = (CheckFrameGen.IsChecked == true) ? 1 : 0;
                int targetHz = 60;
                if (ComboTargetHz.SelectedItem is System.Windows.Controls.ComboBoxItem hzItem && hzItem.Tag != null) {
                    int.TryParse(hzItem.Tag.ToString(), out targetHz);
                }

                StringBuilder sbGen = new StringBuilder();
                sbGen.AppendLine("[General]");
                sbGen.AppendLine($"FrameGen={frameGen}");
                sbGen.AppendLine($"TargetHz={targetHz}");

                File.WriteAllText(framegenCfg, sbGen.ToString());


                // --- Write Legacy Registry (Other Settings) ---
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(RegPath))
                {
                    if (key != null)
                    {
                        // We DO NOT write Width, Height, WINDOW_MODE here anymore.
                        // WINDOW_MODE will be forced to 1 on LaunchButton logic.

                        // VSync
                        key.SetValue("VSync", CheckVsync.IsChecked == true ? 1 : 0, RegistryValueKind.DWord);
                        key.SetValue("BitDepth", 32, RegistryValueKind.DWord);

                        // GraphicCtrl (Preserve existing logic for Shadow, Map, etc)
                        int p0_preset   = 3; // Custom
                        int p1_adv      = CheckAdvEffect.IsChecked == true ? 1 : 0;
                        int p2_shadow   = ComboShadow.SelectedIndex; if (p2_shadow < 0) p2_shadow = 2;
                        int p3_enemy    = 1; // Always High
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
                        key.SetValue("GRAPHICCTRL", graphicCtrlBytes, RegistryValueKind.Binary);

                        // Sound
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
                        key.SetValue("SOUNDCTRL", soundCtrlBytes, RegistryValueKind.Binary);

                        // Save Login
                        key.SetValue("ACCOUNT_CHECK", CheckSaveLogin.IsChecked == true ? 1 : 0, RegistryValueKind.DWord);

                        // Debug Logs (Other non-standard keys) -> Removed from Registry
                        // key.SetValue("DebugLogsEnabled", CheckDebugLogs.IsChecked == true ? 1 : 0, RegistryValueKind.DWord);
                    }
                }
                
                // --- Apply Framework (Always d3d8to9 implicit) ---
                ApplyGraphicsFramework();

                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error saving: " + ex.Message);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left) this.DragMove();
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
                
                // Only copy d3d8.dll if it's missing (First run or deleted)
                // We do NOT want to overwrite it every time as it might be a custom build (like the one the user is debugging)
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
               MessageBox.Show("Framework apply error: " + ex.Message);
            }
        }

        private void CheckFrameGen_Checked(object sender, RoutedEventArgs e)
        {
            if (GridTargetHz != null) GridTargetHz.Visibility = Visibility.Visible;
        }

        private void CheckFrameGen_Unchecked(object sender, RoutedEventArgs e)
        {
             if (GridTargetHz != null) GridTargetHz.Visibility = Visibility.Collapsed;
        }
        
        // ---------------------------------------------------------
        // Gamepad Support (Simple XInput Poll)
        // ---------------------------------------------------------
        private System.Windows.Threading.DispatcherTimer _gamepadTimer;
        private DateTime _lastInputTime = DateTime.MinValue;
        private const int INPUT_DEBOUNCE_MS = 150; // ms between repeats

        private void InitializeGamepad()
        {
            _gamepadTimer = new System.Windows.Threading.DispatcherTimer();
            _gamepadTimer.Interval = TimeSpan.FromMilliseconds(33); // ~30fps poll
            _gamepadTimer.Tick += GamepadTimer_Tick;
            _gamepadTimer.Start();
        }

        private void GamepadTimer_Tick(object sender, EventArgs e)
        {
            XInputState state = new XInputState();
            if (XInputNative.XInputGetState(0, ref state) == 0) // Success
            {
                ProcessGamepadInput(state.Gamepad);
            }
        }

        private void ProcessGamepadInput(XInputGamepad pad)
        {
            if ((DateTime.Now - _lastInputTime).TotalMilliseconds < INPUT_DEBOUNCE_MS)
                return;

            bool inputProcessed = false;
            
            // Stick Threshold
            short deadzone = 8000;
            bool up = (pad.wButtons & 0x0001) != 0 || pad.sThumbLY > deadzone;    // DPAD_UP
            bool down = (pad.wButtons & 0x0002) != 0 || pad.sThumbLY < -deadzone; // DPAD_DOWN
            bool left = (pad.wButtons & 0x0004) != 0 || pad.sThumbLX < -deadzone; // DPAD_LEFT
            bool right = (pad.wButtons & 0x0008) != 0 || pad.sThumbLX > deadzone; // DPAD_RIGHT
            bool aBtn = (pad.wButtons & 0x1000) != 0; // A
            bool bBtn = (pad.wButtons & 0x2000) != 0; // B

            if (up)
            {
                MoveFocus(FocusNavigationDirection.Up);
                inputProcessed = true;
            }
            else if (down)
            {
                MoveFocus(FocusNavigationDirection.Down);
                inputProcessed = true;
            }
            else if (left)
            {
                 MoveFocus(FocusNavigationDirection.Left);
                 inputProcessed = true;
            }
            else if (right)
            {
                 MoveFocus(FocusNavigationDirection.Right);
                 inputProcessed = true;
            }
            else if (aBtn)
            {
                // Simulate Enter on focused element
                 var element = Keyboard.FocusedElement as System.Windows.UIElement;
                 if (element != null)
                 {
                     if (element is System.Windows.Controls.ComboBox cb && !cb.IsDropDownOpen)
                     {
                         cb.IsDropDownOpen = true;
                     }
                     else if (element is System.Windows.Controls.Button btn)
                     {
                        // Programmatic click
                         if (btn.Command != null && btn.Command.CanExecute(btn.CommandParameter))
                             btn.Command.Execute(btn.CommandParameter);
                         else
                             btn.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
                     }
                     else if (element is System.Windows.Controls.CheckBox chk)
                     {
                         chk.IsChecked = !chk.IsChecked;
                     }
                     // If ComboBox DropDown IS open, Arrows handle selection, A should Confirm? 
                     // By default Enter confirms selection in open combobox.
                     // We might need to simulate Key.Enter event if simple invoke doesn't work.
                 }
                 inputProcessed = true;
            }
             else if (bBtn)
            {
                // Back / Close
                // this.Close(); // Maybe too aggressive? Let's just focus Cancel or Close button?
                // Or just do nothing for now unless user requests.
            }

            if (inputProcessed)
                _lastInputTime = DateTime.Now;
        }

        private void MoveFocus(FocusNavigationDirection direction)
        {
            var element = Keyboard.FocusedElement as UIElement;
            if (element != null)
            {
                element.MoveFocus(new TraversalRequest(direction));
            }
            else
            {
                 // Focus first element if nothing focused
                 if (this.Content is UIElement root)
                     root.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            }
        }
    }

    // PInvoke definitions
    internal static class XInputNative
    {
        [System.Runtime.InteropServices.DllImport("xinput1_4.dll", EntryPoint = "XInputGetState")]
        public static extern int XInputGetState(int dwUserIndex, ref XInputState pState);
        
        // Fallback or explicit 9.1.0 (some systems use 1_3, deck usually has newer wine providing all)
        // If 1_4 fails on older user systems, might crash. 
        // Safer to try/catch load or just use standard. Windows 8+ is 1_4.
        // Deck (Proton) supports 1_4.
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct XInputState
    {
        public int PacketNumber;
        public XInputGamepad Gamepad;
    }

    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
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

