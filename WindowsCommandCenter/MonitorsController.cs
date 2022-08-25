using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace WindowsCommandCenter
{
    class MonitorsController : IDisposable
    {
        #region DllImport
        [DllImport("dxva2.dll", EntryPoint = "GetNumberOfPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetNumberOfPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, ref uint pdwNumberOfPhysicalMonitors);

        [DllImport("dxva2.dll", EntryPoint = "GetPhysicalMonitorsFromHMONITOR")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetPhysicalMonitorsFromHMONITOR(IntPtr hMonitor, uint dwPhysicalMonitorArraySize, [Out] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("dxva2.dll", EntryPoint = "GetMonitorBrightness")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetMonitorBrightness(IntPtr handle, ref uint minimumBrightness, ref uint currentBrightness, ref uint maxBrightness);

        [DllImport("dxva2.dll", EntryPoint = "SetMonitorBrightness")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetMonitorBrightness(IntPtr handle, uint newBrightness);

        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitor")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyPhysicalMonitor(IntPtr hMonitor);

        [DllImport("dxva2.dll", EntryPoint = "DestroyPhysicalMonitors")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DestroyPhysicalMonitors(uint dwPhysicalMonitorArraySize, [In] PHYSICAL_MONITOR[] pPhysicalMonitorArray);

        [DllImport("user32.dll")]
        static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, EnumMonitorsDelegate lpfnEnum, IntPtr dwData);
        delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData);
        #endregion

        private IReadOnlyCollection<MonitorInfo> Monitors { get; set; }
        private int brightness;
        private int monitorsCount { get; set; }

        public MonitorsController()
        {
            UpdateMonitors();
            brightness = Get();
        }

        #region Get & Set
        public void Set(uint brightness, int monitorIndex)
        {
            Set(brightness, true, monitorIndex);
        }

        private void Set(uint brightness, bool refreshMonitorsIfNeeded, int monitorIndex = 0)
        {
            bool isSomeFail = false;
            int i = 1;
            foreach (var monitor in Monitors)
            {
                if(monitorIndex == 0 || (monitorIndex == i))
                {
                    uint realNewValue = (monitor.MaxValue - monitor.MinValue) * brightness / 100 + monitor.MinValue;
                    if (SetMonitorBrightness(monitor.Handle, realNewValue))
                    {
                        monitor.CurrentBrightness = realNewValue;
                    }
                    else if (refreshMonitorsIfNeeded)
                    {
                        isSomeFail = true;
                        break;
                    }
                }
                i++;
            }

            if (refreshMonitorsIfNeeded && (isSomeFail || !Monitors.Any()))
            {
                UpdateMonitors();
                Set(brightness, false, monitorIndex);
                return;
            }
        }

        public int Get()
        {
            if (!Monitors.Any())
            {
                return -1;
            }
            return (int)Monitors.Average(d => d.CurrentBrightness);
        }

        public int getBrightness()
        {
            return this.brightness;
        }

        public void setBrightness(int brightness, int monitorIndex)
        {
            if (brightness > 100)
                brightness = 100;
            else if (brightness < 0)
                brightness = 0;

            if (brightness == this.brightness)
                return;

            Set(Convert.ToUInt32(brightness), monitorIndex);
            this.brightness = brightness;
        }

        public void brightnessIncrease(int monitorIndex)
        {
            foreach(var monitor in Monitors)
            {
                if(monitorIndex == 0 || (monitorIndex == monitor.Id))
                {
                    int newVal = (int)monitor.CurrentBrightness + 3;
                    newVal = (newVal < 0) ? 0 : (newVal > 100) ? 100 : newVal;

                    Set(Convert.ToUInt32(newVal), monitorIndex);

                    monitor.CurrentBrightness = (uint)newVal;
                }
            }

            //int newVal = this.brightness + 3;
            //newVal = (newVal < 0) ? 0 : (newVal > 100) ? 100 : newVal;

            //Set(Convert.ToUInt32(newVal), monitorIndex);
            //this.brightness = newVal;
        }
        #endregion

        public int getMonitorsCount()
        {
            return this.monitorsCount;
        }

        public IReadOnlyCollection<MonitorInfo> getMonitors()
        {
            return this.Monitors;
        }

        private void UpdateMonitors()
        {
            DisposeMonitors(this.Monitors);
            monitorsCount = 0;

            var monitors = new List<MonitorInfo>();
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr hMonitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr dwData) =>
            {
                uint physicalMonitorsCount = 0;
                if (!GetNumberOfPhysicalMonitorsFromHMONITOR(hMonitor, ref physicalMonitorsCount))
                {
                    // Cannot get monitor count
                    return true;
                }

                var physicalMonitors = new PHYSICAL_MONITOR[physicalMonitorsCount];
                if (!GetPhysicalMonitorsFromHMONITOR(hMonitor, physicalMonitorsCount, physicalMonitors))
                {
                    // Cannot get physical monitor handle
                    return true;
                }

                foreach (PHYSICAL_MONITOR physicalMonitor in physicalMonitors)
                {
                    uint minValue = 0, CurrentBrightness = 0, maxValue = 0;
                    if (!GetMonitorBrightness(physicalMonitor.hPhysicalMonitor, ref minValue, ref CurrentBrightness, ref maxValue))
                    {
                        DestroyPhysicalMonitor(physicalMonitor.hPhysicalMonitor);
                        continue;
                    }

                    var info = new MonitorInfo
                    {
                        Id = monitorsCount+1,
                        Handle = physicalMonitor.hPhysicalMonitor,
                        MinValue = minValue,
                        CurrentBrightness = CurrentBrightness,
                        MaxValue = maxValue,
                    };
                    monitors.Add(info);
                    monitorsCount++;
                }

                return true;
            }, IntPtr.Zero);

            this.Monitors = monitors;
        }

        public void Dispose()
        {
            DisposeMonitors(Monitors);
            GC.SuppressFinalize(this);
        }

        private static void DisposeMonitors(IEnumerable<MonitorInfo> monitors)
        {
            if (monitors?.Any() == true)
            {
                PHYSICAL_MONITOR[] monitorArray = monitors.Select(m => new PHYSICAL_MONITOR { hPhysicalMonitor = m.Handle }).ToArray();
                DestroyPhysicalMonitors((uint)monitorArray.Length, monitorArray);
            }
        }

        #region Classes
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct PHYSICAL_MONITOR
        {
            public IntPtr hPhysicalMonitor;

            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string szPhysicalMonitorDescription;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Rect
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        public class MonitorInfo
        {
            public int Id { get; set; }
            public uint MinValue { get; set; }
            public uint MaxValue { get; set; }
            public IntPtr Handle { get; set; }
            public uint CurrentBrightness { get; set; }
        }
        #endregion
    }
}
