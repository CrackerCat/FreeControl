﻿using FreeControl.Utils;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FreeControl
{
    public partial class Main : UIForm
    {
        public Main()
        {
            InitializeComponent();
            InitPdone();
        }

        /// <summary>
        /// 用户数据目录
        /// </summary>
        public static string UserDataPath
        {
            get
            {
                return Path.Combine(System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Free Control");
            }
        }

        public Setting GetUserData()
        {
            try
            {
                var tempData = new Setting();
                var fullPath = Path.Combine(UserDataPath, "config.json");
                Directory.CreateDirectory(UserDataPath);
                if (!File.Exists(fullPath))
                {
                    File.WriteAllText(fullPath, JsonHelper.json(tempData));
                }
                StreamReader reader = File.OpenText(fullPath);
                tempData = JsonHelper.jsonDes<Setting>(reader.ReadToEnd());
                reader.Close();
                return tempData;
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
                return new Setting();
            }
        }

        public void SetUserData(Setting userData)
        {
            try
            {
                var fullPath = Path.Combine(UserDataPath, "config.json");
                Directory.CreateDirectory(UserDataPath);
                File.WriteAllText(fullPath, JsonHelper.json(userData));
            }
            catch (Exception ex)
            {
                LogHelper.Error(ex);
            }
        }

        Setting setting = new Setting();
        ProcessStartInfo AdbProcessInfo = null;

        /// <summary>
        /// scrcpy版本
        /// </summary>
        public static string scrcpyVersion = "scrcpy_win32_v1_18";

        /// <summary>
        /// scrcpy路径
        /// </summary>
        public static string scrcpyPath = Path.Combine(UserDataPath, scrcpyVersion + "\\");

        public void InitPdone()
        {

            setting = GetUserData();
            Size = new Size(658, 340);

            Application.ApplicationExit += (sender, e) =>
            {
                SetUserData(setting);
            };

            this.FormClosed += (sender, e) =>
            {
                if (AdbProcessInfo != null)
                {
                    //退出前关闭adb
                    AdbProcessInfo.Arguments = "kill-server";
                    Process.Start(AdbProcessInfo);
                    LogHelper.Info("kill adb server");
                }
                Application.Exit();
            };
            #region 设置标题
            Assembly asm = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(asm.Location);
            //Text = $"Free Control v{fvi.ProductVersion}";
            //ledTitle.Enabled = false;
            //ledTitle.Visible = false;
            ledTitle.Text = $"Free Control v{fvi.ProductVersion}";
            ledTitle.CharCount = 19;
            ledTitle.MouseDown += (sender, e) => DragWindow();
            this.Icon = Properties.Resources.pcm;
            #endregion

            #region 设置主题颜色
            UIStyles.SetStyle(UIStyle.Gray);

            //设置默认导航条颜色
            navTab.TabSelectedForeColor = Color.FromArgb(140, 140, 140);
            navTab.TabSelectedHighColor = Color.FromArgb(140, 140, 140);
            //设置默认导航条图标
            tabHome.ImageIndex = 0;
            tabSetting.ImageIndex = 2;
            #endregion
            #region 设置深色模式
            switchDarkMode.ValueChanged += (object sender, bool value) =>
            {
                var tabBackColor = Color.Transparent;
                var foreColor = Color.Transparent;
                if (value)
                {
                    tabBackColor = Color.FromArgb(24, 24, 24);
                    foreColor = Color.FromArgb(192, 192, 192);
                    UIStyles.SetStyle(UIStyle.Black);
                    tabHome.BackColor = tabBackColor;
                    tabSetting.BackColor = tabBackColor;
                    navTab.MenuStyle = UIMenuStyle.Black;

                    btnStart.SetStyle(UIStyle.Black);

                    tabHome.ImageIndex = 1;
                    tabSetting.ImageIndex = 3;
                }
                else
                {
                    tabBackColor = Color.FromArgb(242, 242, 244);
                    foreColor = Color.FromArgb(48, 48, 48);
                    UIStyles.SetStyle(UIStyle.Gray);
                    tabHome.BackColor = tabBackColor;
                    tabSetting.BackColor = tabBackColor;
                    navTab.MenuStyle = UIMenuStyle.White;

                    btnStart.SetStyle(UIStyle.Gray);

                    tabHome.ImageIndex = 0;
                    tabSetting.ImageIndex = 2;

                    navTab.TabSelectedColor = tabBackColor;
                    navTab.TabSelectedForeColor = Color.FromArgb(140, 140, 140);
                    navTab.TabSelectedHighColor = Color.FromArgb(140, 140, 140);
                    navTab.TabBackColor = Color.FromArgb(222, 222, 222);

                }

                tbxAddress.FillDisableColor = tabBackColor;
                tbxPort.FillDisableColor = tabBackColor;
                tbxAddress.ForeDisableColor = foreColor;
                tbxPort.ForeDisableColor = foreColor;

                tbxAddress.FillColor = tabBackColor;
                tbxPort.FillColor = tabBackColor;
                tbxAddress.ForeColor = foreColor;
                tbxPort.ForeColor = foreColor;

            };
            #endregion

            #region 切换tab事件
            navTab.SelectedIndexChanged += (object sender, EventArgs e) =>
            {
                switch (navTab.SelectedIndex)
                {
                    case 0:
                        Size = new Size(658, 340);
                        break;
                    case 1:
                        Size = new Size(658, 470);
                        break;
                    default:
                        break;
                }
            };
            navTab.SelectTab(0);
            #endregion

            #region 设置默认值
            rbtnPx.SelectedIndex = setting.PXIndex;
            rbtnMbps.SelectedIndex = setting.BitRateIndex;
            rbtnMaxFPS.SelectedIndex = setting.MaxFPSIndex;
            rbtnShortcuts.SelectedIndex = setting.ShortcutsIndex;
            switchDarkMode.Active = setting.DarkMode;
            tbxAddress.Text = setting.IPAddress;
            tbxPort.Text = setting.Port;
            //tbxAddress.Enabled = !setting.UseWireless;
            //tbxPort.Enabled = !setting.UseWireless;
            updownHeight.Value = setting.WindowHeight;
            updownWidth.Value = setting.WindowWidth;

            #region 复选框默认值   
            cbxUseWireless.Checked = setting.UseWireless;
            cbxUseLog.Checked = setting.UseLog;
            LogHelper.enable = setting.UseLog;

            cbxCloseScreen.Checked = setting.CloseScreen;
            cbxKeepAwake.Checked = setting.KeepAwake;
            cbxAllFPS.Checked = setting.AllFPS;

            cbxHideBorder.Checked = setting.HideBorder;
            cbxFullScreen.Checked = setting.FullScreen;
            cbxTopMost.Checked = setting.TopMost;
            cbxShowTouches.Checked = setting.ShowTouches;
            cbxReadOnly.Checked = setting.ReadOnly;
            #endregion

            #region 参数设置事件
            rbtnPx.ValueChanged += RbtnPx_ValueChanged;
            rbtnMbps.ValueChanged += RbtnMbps_ValueChanged;
            rbtnMaxFPS.ValueChanged += RbtnMaxFPS_ValueChanged;
            rbtnShortcuts.ValueChanged += RbtnShortcuts_ValueChanged;

            cbxUseWireless.ValueChanged += CbxUseWireless_ValueChanged;
            cbxUseLog.ValueChanged += CbxUseLog_ValueChanged; ;

            switchDarkMode.ValueChanged += (sender, e) =>
            {
                setting.DarkMode = switchDarkMode.Active;
            };
            tbxAddress.TextChanged += TbxAddress_TextChanged;
            tbxPort.TextChanged += TbxPort_TextChanged;

            cbxCloseScreen.ValueChanged += CommonCbx_ValueChanged;
            cbxKeepAwake.ValueChanged += CommonCbx_ValueChanged;
            cbxAllFPS.ValueChanged += CommonCbx_ValueChanged;
            cbxHideBorder.ValueChanged += CommonCbx_ValueChanged;
            cbxFullScreen.ValueChanged += CommonCbx_ValueChanged;
            cbxTopMost.ValueChanged += CommonCbx_ValueChanged;
            cbxShowTouches.ValueChanged += CommonCbx_ValueChanged;
            cbxReadOnly.ValueChanged += CommonCbx_ValueChanged;

            updownHeight.ValueChanged += (sender, e) =>
            {
                setting.WindowHeight = updownHeight.Value;
            };
            updownWidth.ValueChanged += (sender, e) =>
            {
                setting.WindowWidth = updownWidth.Value;
            };
            #endregion

            #endregion

            #region 启动前
            string tempFileName = "temp.zip";
            if (!Directory.Exists(scrcpyPath))
            {
                Directory.CreateDirectory(scrcpyPath);
                File.WriteAllBytes(scrcpyPath + tempFileName, Properties.Resources.scrcpy_win32_v1_18);
                if (SharpZip.UnpackFiles(scrcpyPath + tempFileName, scrcpyPath))
                {
                    File.Delete(scrcpyPath + tempFileName);
                }
            }
            #endregion

            Process scrcpy = null;

            //设置端口号命令 adb tcpip 5555
            #region 启动按钮
            btnStart.Click += (sender, e) =>
            {

                if (setting.UseWireless &&
                (string.IsNullOrWhiteSpace(setting.IPAddress) || string.IsNullOrWhiteSpace(setting.Port)))
                {
                    UIMessageTip.ShowWarning(sender as Control, "IP地址或者端口号没有填写，无法启动 -.-!", 1500);
                    return;
                }
                LogHelper.Info("starting...");
                var paramlist = $" {setting.BitRate} {setting.PX} {setting.MaxFPS} {setting.Shortcuts} {setting.OtherParam} ";
                //设置屏幕高度 800
                if (setting.WindowHeight > 0)
                {
                    paramlist += $"--window-height {setting.WindowHeight} ";
                }
                if (setting.WindowWidth > 0)
                {
                    paramlist += $"--window-width {setting.WindowWidth} ";
                }

                //设置标题
                paramlist += $"--window-title \"{ledTitle.Text}\" ";

                AdbProcessInfo = new ProcessStartInfo($@"{scrcpyPath}adb.exe",
                    $"connect {setting.IPAddress}:{setting.Port}")
                {
                    CreateNoWindow = true,//设置不在新窗口中启动新的进程        
                    UseShellExecute = false,//不使用操作系统使用的shell启动进程 
                    RedirectStandardOutput = true,//将输出信息重定向
                };

                if (setting.UseWireless)
                {
                    //启动ABD
                    Process adb = Process.Start(AdbProcessInfo);
                    LogHelper.Info(adb.StandardOutput.ReadToEnd());
                    adb.WaitForExit();
                    paramlist = $"-s {setting.IPAddress}:{setting.Port} " + paramlist;
                }

                scrcpy = Process.Start(new ProcessStartInfo($@"{scrcpyPath}scrcpy.exe",
                    paramlist)
                {
                    CreateNoWindow = true,//设置不在新窗口中启动新的进程        
                    UseShellExecute = false,//不使用操作系统使用的shell启动进程 
                    RedirectStandardOutput = true,//将输出信息重定向
                });

                this.Hide();
                LogHelper.Info("scrcpy running...");
                LogHelper.Info(scrcpy.StandardOutput.ReadToEnd());

                scrcpy.WaitForExit();
                UIMessageTip.Show(this, "已退出", null, 1500);
                this.Show();

            };
            #endregion
        }

        private void CbxUseLog_ValueChanged(object sender, bool value)
        {
            setting.UseLog = value;
            LogHelper.enable = value;
        }

        private void CbxUseWireless_ValueChanged(object sender, bool value)
        {
            setting.UseWireless = value;
            //tbxAddress.Enabled = !value;
            //tbxPort.Enabled = !value;

            var foreColor = Color.Transparent;
            var tabBackColor = Color.Transparent;

            var tempColor = Color.Transparent;
            if (setting.DarkMode)
            {
                tabBackColor = Color.FromArgb(24, 24, 24);
                foreColor = Color.FromArgb(192, 192, 192);
            }
            else
            {
                tabBackColor = Color.FromArgb(242, 242, 244);
                foreColor = Color.FromArgb(48, 48, 48);
            }

            if (setting.UseWireless)
            {
                tbxAddress.FillDisableColor = tabBackColor;
                tbxPort.FillDisableColor = tabBackColor;
                tbxAddress.ForeDisableColor = foreColor;
                tbxPort.ForeDisableColor = foreColor;
            }
            else
            {
                tbxAddress.FillColor = tabBackColor;
                tbxPort.FillColor = tabBackColor;
                tbxAddress.ForeColor = foreColor;
                tbxPort.ForeColor = foreColor;
            }
        }

        private void TbxAddress_TextChanged(object sender, EventArgs e)
        {
            //if (!tbxAddress.Text.IsNullOrWhiteSpace())
            //{
            setting.IPAddress = tbxAddress.Text.Trim();
            //}
        }
        private void TbxPort_TextChanged(object sender, EventArgs e)
        {
            //if (!tbxPort.Text.IsNullOrWhiteSpace())
            //{
            setting.Port = tbxPort.Text.Trim();
            //}
        }

        #region 参数设置事件
        private void CommonCbx_ValueChanged(object sender, bool value)
        {
            string command = "";
            var temp = sender as UICheckBox;
            switch (temp.Text)
            {
                case "关闭屏幕":
                    setting.CloseScreen = value;
                    //关闭屏幕与镜像模式不可同时启用
                    setting.ReadOnly = !value;
                    command = setting.GetDesc("CloseScreen") + " ";
                    break;
                case "保持唤醒":
                    command = setting.GetDesc("KeepAwake") + " ";
                    setting.KeepAwake = value;
                    //保持唤醒与镜像模式不可同时启用
                    setting.ReadOnly = !value;
                    cbxReadOnly.Checked = !value;
                    break;
                case "全帧渲染":
                    command = setting.GetDesc("AllFPS") + " ";
                    setting.AllFPS = value;
                    break;
                case "镜像模式":
                    command = setting.GetDesc("ReadOnly") + " ";
                    setting.ReadOnly = value;
                    setting.CloseScreen = !value;
                    setting.KeepAwake = !value;
                    break;
                case "隐藏边框":
                    command = setting.GetDesc("HideBorder") + " ";
                    setting.HideBorder = value;
                    break;
                case "全屏显示":
                    command = setting.GetDesc("FullScreen") + " ";
                    setting.FullScreen = value;
                    break;
                case "窗口置顶":
                    command = setting.GetDesc("TopMost") + " ";
                    setting.TopMost = value;
                    break;
                case "显示触摸":
                    command = setting.GetDesc("ShowTouches") + " ";
                    setting.ShowTouches = value;
                    break;
            }
            LogHelper.Info(temp.Text + ":" + value);
            if (value)
            {
                setting.OtherParam += command;
            }
            else
            {
                setting.OtherParam = setting.OtherParam.Replace(command, "");
            }
        }

        private void RbtnShortcuts_ValueChanged(object sender, int index, string text)
        {
            switch (index)
            {
                case 1:
                    setting.Shortcuts = "lctrl+lalt";
                    break;
                case 2:
                    setting.Shortcuts = "lalt";
                    break;
                default:
                    setting.Shortcuts = "lctrl";
                    break;
            }
            setting.Shortcuts = $"{setting.GetDesc("Shortcuts")}={setting.Shortcuts}";
            setting.ShortcutsIndex = index;
        }

        private void RbtnMaxFPS_ValueChanged(object sender, int index, string text)
        {
            switch (index)
            {
                case 1:
                    setting.MaxFPS = "--max-fps 140";
                    break;
                case 2:
                    setting.MaxFPS = "--max-fps 120";
                    break;
                case 3:
                    setting.MaxFPS = "--max-fps 90";
                    break;
                case 4:
                    setting.MaxFPS = "--max-fps 60";
                    break;
                case 5:
                    setting.MaxFPS = "--max-fps 30";
                    break;
                default:
                    setting.MaxFPS = "";
                    break;
            }
            setting.MaxFPSIndex = index;
        }

        private void RbtnPx_ValueChanged(object sender, int index, string text)
        {
            switch (index)
            {
                case 1:
                    setting.PX = "-m 1920";
                    break;
                case 2:
                    setting.PX = "-m 1440";
                    break;
                case 3:
                    setting.PX = "-m 1280";
                    break;
                case 4:
                    setting.PX = "-m 960";
                    break;
                case 5:
                    setting.PX = "-m 640";
                    break;
                default:
                    setting.PX = "";
                    break;
            }
            setting.PXIndex = index;
        }

        private void RbtnMbps_ValueChanged(object sender, int index, string text)
        {
            switch (index)
            {
                case 1:
                    setting.BitRate = "-b 64M";
                    break;
                case 2:
                    setting.BitRate = "-b 32M";
                    break;
                case 3:
                    setting.BitRate = "-b 16M";
                    break;
                case 4:
                    setting.BitRate = "-b 8M";
                    break;
                case 5:
                    setting.BitRate = "-b 4M";
                    break;
                default:
                    setting.BitRate = "";
                    break;
            }
            setting.BitRateIndex = index;
        }
        #endregion

        #region 拖动窗口
        [DllImport("user32.dll")]//拖动无窗体的控件
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        public const int WM_SYSCOMMAND = 0x0112;
        public const int SC_MOVE = 0xF010;
        public const int HTCAPTION = 0x0002;

        /// <summary>
        /// 拖动窗体
        /// </summary>
        public void DragWindow()
        {
            ReleaseCapture();
            SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE + HTCAPTION, 0);
        }
        #endregion

        private void uiLabel2_Click(object sender, EventArgs e)
        {
            Form shortcut = new Form()
            {
                AutoSize = true,
                StartPosition = FormStartPosition.CenterScreen,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                MinimizeBox = false,
                MaximizeBox = false,
            };
            shortcut.KeyPress += (senderr, ee) =>
            {
                if (ee.KeyChar == (char)Keys.Escape)
                {
                    shortcut.Close();
                }
            };
            PictureBox pictureBox = new PictureBox
            {
                Image = Properties.Resources.shortcut_zh,
                SizeMode = PictureBoxSizeMode.AutoSize,
            };
            shortcut.Controls.Add(pictureBox);
            shortcut.ShowDialog();
        }

        private void uiLinkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            System.Diagnostics.Process.Start("https://baike.baidu.com/item/USB%E8%B0%83%E8%AF%95%E6%A8%A1%E5%BC%8F/5035286#2");
        }

        private void uiLinkLabel2_Click(object sender, EventArgs e)
        {
            if (UIMessageBox.Show("1、使用数据线将手机连接电脑\n2、手机开启调试模式\n3、程序将使用adb tcpip 5555命令修改无线调试端口号\n4、点击确定后若看到一只狗头，则表示设置端口号成功", "请确认", setting.DarkMode ? UIStyle.Black : UIStyle.Gray, UIMessageBoxButtons.OKCancel, false))
            {
                var batPath = scrcpyPath + "SetProt.bat";
                if (!File.Exists(batPath))
                {
                    //提取嵌入资源
                    FileHelper.ExtractResFile("FreeControl.SetProt.bat", batPath);
                }
                if (File.Exists(batPath))
                {
                    System.Diagnostics.Process.Start(batPath, scrcpyVersion);
                }
            }
        }

        private void uiLinkLabel3_LinkClicked(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://github.com/pdone/FreeControl/issues");
        }
    }

    public static class Utilitys
    {
        public static bool IsNullOrWhiteSpace(this string s)
        {
            return string.IsNullOrWhiteSpace(s);
        }

        public static string GetDesc<T>(this T obj, string name)
        {
            T ent = obj;
            var res = "";
            foreach (var item in ent.GetType().GetProperties())
            {
                if (item.Name != name)
                {
                    continue;
                }
                var v = (DescriptionAttribute[])item.GetCustomAttributes(typeof(DescriptionAttribute), false);
                if (v != null && v.Count() > 0)
                {
                    res = v[0].Description;
                    return res;
                }
            }
            return res;
        }

        public static string GetEnumDesc<T>(this T obj)
        {
            var type = obj.GetType();
            FieldInfo field = type.GetField(Enum.GetName(type, obj));
            DescriptionAttribute descAttr = Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute)) as DescriptionAttribute;
            if (descAttr == null)
            {
                return string.Empty;
            }

            return descAttr.Description;
        }
    }
}
