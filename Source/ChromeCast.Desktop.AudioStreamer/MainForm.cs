﻿using System;
using System.Windows.Forms;
using ChromeCast.Desktop.AudioStreamer.Application;
using ChromeCast.Desktop.AudioStreamer.UserControls;
using ChromeCast.Desktop.AudioStreamer.Application.Interfaces;
using System.Threading.Tasks;
using CSCore.CoreAudioAPI;
using ChromeCast.Desktop.AudioStreamer.Classes;
using System.Net;
using System.Net.NetworkInformation;
using System.Linq;
using System.Reflection;
using System.Diagnostics;

namespace ChromeCast.Desktop.AudioStreamer
{
    public partial class MainForm : Form, IMainForm
    {
        private IApplicationLogic applicationLogic;
        private IDevices devices;
        private ILogger logger;

        public MainForm(IApplicationLogic applicationLogicIn, IDevices devicesIn, ILogger loggerIn)
        {
            InitializeComponent();

            applicationLogic = applicationLogicIn;
            devices = devicesIn;
            logger = loggerIn;
            logger.SetCallback(Log);
            devices.SetDependencies(this, applicationLogic);
            applicationLogic.SetDependencies(this);
        }

        public MainForm()
        {
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            Update();
            AddIP4Addresses();
            applicationLogic.Start();
            NetworkChange.NetworkAddressChanged += new NetworkAddressChangedEventHandler(AddressChangedCallback);

            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            lblVersion.Text = $"Version {fvi.FileVersion}";
        }

        private void AddressChangedCallback(object sender, EventArgs e)
        {
            AddIP4Addresses();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            applicationLogic.CloseApplication();
        }

        public void AddDevice(IDevice device)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<Device>(AddDevice), new object[] { device });
                return;
            }
            if (IsDisposed) return;

            var deviceControl = new DeviceControl(device);
            deviceControl.SetDeviceName(device.GetFriendlyName());
            deviceControl.SetClickCallBack(device.OnClickDeviceButton);
            deviceControl.SetStatus(device.GetDeviceState(), null);
            pnlDevices.Controls.Add(deviceControl);
            device.SetDeviceControl(deviceControl);

            // Sort alphabetically.
            var deviceName = device.GetFriendlyName();
            for (int i = 0; i < pnlDevices.Controls.Count - 1; i++)
            {
                if (pnlDevices.Controls[i] is DeviceControl)
                {
                    var name = ((DeviceControl)pnlDevices.Controls[i]).GetDeviceName();
                    if (string.CompareOrdinal(deviceName, name) < 0)
                    {
                        pnlDevices.Controls.SetChildIndex(deviceControl, i);
                        return;
                    }
                }
            }
        }

        public void ShowLagControl(bool showLag)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(ShowLagControl), new object[] { showLag });
                return;
            }
            if (IsDisposed) return;

            if (!showLag)
            {
                grpDevices.Height += grpLag.Height;
                pnlDevices.Height += grpLag.Height;
                btnScan.Top = grpDevices.Height - btnScan.Height - 10;
            }
            grpLag.Visible = showLag;
        }

        public void ShowLog(bool boolShowLog)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(ShowLog), new object[] { boolShowLog });
                return;
            }
            if (IsDisposed) return;

            if (!boolShowLog)
                tabControl.TabPages.RemoveAt(2);
        }

        public void SetLagValue(int lagValue)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<int>(SetLagValue), new object[] { lagValue });
                return;
            }
            if (IsDisposed) return;

            trbLag.Value = lagValue;
        }

        public void SetKeyboardHooks(bool useShortCuts)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<bool>(SetKeyboardHooks), new object[] { useShortCuts });
                return;
            }
            if (IsDisposed) return;

            chkHook.Checked = useShortCuts;
        }

        public void ToggleVisibility()
        {
            if (Visible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
                WindowState = FormWindowState.Normal;
            }
        }

        public void Log(string message)
        {
            try
            {
                if (InvokeRequired)
                {
                    Invoke(new Action<string>(Log), new object[] { message });
                    return;
                }
                if (IsDisposed) return;

                if (message.Contains("\"type\":\"PONG\"") || message.Contains("\"type\":\"PING\""))
                {
                    labelPingPong.Text = "Latest keep-alive message:" + DateTime.Now.ToLongTimeString();
                    labelPingPong.Update();
                }
                else
                {
                    textLog.AppendText(message + "\r\n\r\n");
                }
            }
            catch (Exception)
            {
            }
        }

        private void trbLag_Scroll(object sender, EventArgs e)
        {
            applicationLogic.SetLagThreshold(trbLag.Value);
        }

        private void chkHook_CheckedChanged(object sender, EventArgs e)
        {
            applicationLogic.OnSetHooks(chkHook.Checked);

        }

        private void btnVolumeUp_Click(object sender, EventArgs e)
        {
            devices.VolumeUp();
        }

        private void btnVolumeDown_Click(object sender, EventArgs e)
        {
            devices.VolumeDown();
        }

        private void btnVolumeMute_Click(object sender, EventArgs e)
        {
            devices.VolumeMute();
        }

        public async void SetWindowVisibility(bool visible)
        {
            chkShowWindowOnStart.Checked = visible;
            if (visible)
                Show();
            else
            {
                await HideWindow();
            }
        }

        private async Task HideWindow()
        {
            await Task.Delay(1000);
            Hide();
        }

        public bool DoSyncDevices()
        {
            return devices.Count() > 1;
        }

        private void btnSyncDevices_Click(object sender, EventArgs e)
        {
            devices.Sync();
        }

        public void AddRecordingDevices(MMDeviceCollection devices, MMDevice defaultdevice)
        {
            if (InvokeRequired)
            {
                Invoke(new Action<MMDeviceCollection, MMDevice>(AddRecordingDevices), new object[] { devices, defaultdevice });
                return;
            }
            if (IsDisposed) return;

            foreach (var device in devices)
            {
                if (!cmbRecordingDevice.Items.Contains(device))
                {
                    var index = cmbRecordingDevice.Items.Add(device);
                    if (device.DeviceID == defaultdevice.DeviceID)
                        cmbRecordingDevice.SelectedIndex = index;
                }
            }
        }

        public MMDevice GetRecordingDevice()
        {
            if (InvokeRequired)
            {
                try
                {
                    if (cmbRecordingDevice.Items.Count > 0)
                        return (MMDevice)cmbRecordingDevice.SelectedItem;
                }
                catch (Exception ex)
                {
                    logger.Log($"ex : {ex.Message}");
                    Invoke(new Func<MMDevice>(GetRecordingDevice));
                    return null;
                }
            }
            if (IsDisposed) return null;

            if (cmbRecordingDevice.Items.Count > 0)
                return (MMDevice)cmbRecordingDevice.SelectedItem;

            return null;
        }

        private void cmbRecordingDevice_SelectedIndexChanged(object sender, EventArgs e)
        {
            applicationLogic.RecordingDeviceChanged();
        }

        private void chkAutoRestart_CheckedChanged(object sender, EventArgs e)
        {
            applicationLogic.OnSetAutoRestart(chkAutoRestart.Checked);
        }

        public void SetAutoRestart(bool autoRestart)
        {
            chkAutoRestart.Checked = autoRestart;
        }

        public void AddIP4Addresses()
        {
            if (InvokeRequired)
            {
                Invoke(new Action(AddIP4Addresses));
                return;
            }

            var oldAddressUsed = (IPAddress)cmbIP4AddressUsed.SelectedItem;
            var ip4Adresses = Network.GetIp4ddresses();
            foreach (var adapter in ip4Adresses)
            {
                if (cmbIP4AddressUsed.Items.IndexOf(adapter.IPAddress) < 0)
                    cmbIP4AddressUsed.Items.Add(adapter.IPAddress);
            }
            for (int i = cmbIP4AddressUsed.Items.Count - 1; i >= 0; i--)
            {
                if (!ip4Adresses.Any(x => x.IPAddress.ToString() == ((IPAddress)cmbIP4AddressUsed.Items[i]).ToString()))
                    cmbIP4AddressUsed.Items.RemoveAt(i);
            }

            if (!ip4Adresses.Any(x => x.IPAddress.ToString() == oldAddressUsed?.ToString()))
            {
                var addressUsed = Network.GetIp4Address();
                cmbIP4AddressUsed.SelectedItem = addressUsed;
            }
        }

        private void cmbIP4AddressUsed_SelectedIndexChanged(object sender, EventArgs e)
        {
            var ipAddress = (IPAddress)cmbIP4AddressUsed.SelectedItem;
            applicationLogic.ChangeIPAddressUsed(ipAddress);
        }

        private void btnClipboardCopy_Click(object sender, EventArgs e)
        {
            Clipboard.SetText(textLog.Text);
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            applicationLogic.ScanForDevices();
        }

        public void SetAutoStart(bool autoStart)
        {
            chkAutoStart.Checked = autoStart;
        }

        public bool GetUseKeyboardShortCuts()
        {
            return chkHook.Checked;
        }

        public bool GetAutoStartDevices()
        {
            return chkAutoStart.Checked;
        }

        public bool GetShowWindowOnStart()
        {
            return chkShowWindowOnStart.Checked;
        }

        public bool GetAutoRestart()
        {
            return chkAutoRestart.Checked;
        }

        private void btnResetSettings_Click(object sender, EventArgs e)
        {
            applicationLogic.ResetSettings();
        }
    }
}
