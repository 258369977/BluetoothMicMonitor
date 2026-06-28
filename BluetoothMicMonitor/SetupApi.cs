using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace BluetoothMicMonitor
{
    internal static class SetupApi
    {
        public static readonly Guid MediaClassGuid = new Guid("4d36e96c-e325-11ce-bfc1-08002be10318");

        private const uint DIGCF_PRESENT = 0x02;
        private const uint SPDRP_FRIENDLYNAME = 0x0C;
        private const uint DIF_PROPERTYCHANGE = 0x12;
        private const uint DICS_ENABLE = 0x01;
        private const uint DICS_DISABLE = 0x02;
        private const uint DICS_FLAG_GLOBAL = 0x01;

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_CLASSINSTALL_HEADER
        {
            public uint cbSize;
            public uint InstallFunction;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_DEVINFO_DATA
        {
            public uint cbSize;
            public Guid ClassGuid;
            public uint DevInst;
            public IntPtr Reserved;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct SP_PROPCHANGE_PARAMS
        {
            public SP_CLASSINSTALL_HEADER ClassInstallHeader;
            public uint StateChange;
            public uint Scope;
            public uint HwProfile;
        }

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern IntPtr SetupDiGetClassDevs(
            ref Guid ClassGuid, string Enumerator, IntPtr hwndParent, uint Flags);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiDestroyDeviceInfoList(IntPtr DeviceInfoSet);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiEnumDeviceInfo(
            IntPtr DeviceInfoSet, uint MemberIndex, ref SP_DEVINFO_DATA DeviceInfoData);

        [DllImport("setupapi.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool SetupDiGetDeviceRegistryProperty(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            uint Property,
            out uint PropertyRegDataType,
            IntPtr PropertyBuffer,
            uint PropertyBufferSize,
            out uint RequiredSize);

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiSetClassInstallParams(
            IntPtr DeviceInfoSet,
            ref SP_DEVINFO_DATA DeviceInfoData,
            ref SP_PROPCHANGE_PARAMS ClassInstallParams,
            uint ClassInstallParamsSize);

        [DllImport("setupapi.dll")]
        private static extern int CM_Get_DevNode_Status(
            out uint pulStatus, out uint pulProblemNumber,
            uint dnDevInst, uint ulFlags);

        private const uint DN_STARTED = 0x00000008;

        [DllImport("setupapi.dll", SetLastError = true)]
        private static extern bool SetupDiCallClassInstaller(
            uint InstallFunction, IntPtr DeviceInfoSet, ref SP_DEVINFO_DATA DeviceInfoData);

        public static bool SetDeviceState(string friendlyName, bool enable, out string error)
        {
            error = null;
            var guid = MediaClassGuid;

            var hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT);
            if (hDevInfo == IntPtr.Zero || hDevInfo == unchecked((IntPtr)(-1)))
            {
                error = "SetupDiGetClassDevs failed (" + Marshal.GetLastWin32Error() + ")";
                return false;
            }

            try
            {
                var di = default(SP_DEVINFO_DATA);
                di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                uint idx = 0;

                while (SetupDiEnumDeviceInfo(hDevInfo, idx, ref di))
                {
                    uint sz = 0;
                    uint rdt = 0;
                    SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                        out rdt, IntPtr.Zero, 0, out sz);

                    if (sz > 0)
                    {
                        var buf = Marshal.AllocHGlobal((int)sz);
                        try
                        {
                            uint rdt2 = 0;
                            if (SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                                out rdt2, buf, sz, out sz))
                            {
                                var name = Marshal.PtrToStringUni(buf);
                                if (name != null && name.Equals(friendlyName, StringComparison.OrdinalIgnoreCase))
                                {
                                    var pc = default(SP_PROPCHANGE_PARAMS);
                                    pc.ClassInstallHeader.cbSize = (uint)Marshal.SizeOf(typeof(SP_CLASSINSTALL_HEADER));
                                    pc.ClassInstallHeader.InstallFunction = DIF_PROPERTYCHANGE;
                                    pc.StateChange = enable ? DICS_ENABLE : DICS_DISABLE;
                                    pc.Scope = DICS_FLAG_GLOBAL;

                                    if (!SetupDiSetClassInstallParams(hDevInfo, ref di, ref pc,
                                        (uint)Marshal.SizeOf(typeof(SP_PROPCHANGE_PARAMS))))
                                    {
                                        error = "SetClassInstallParams failed (" + Marshal.GetLastWin32Error() + ")";
                                        return false;
                                    }

                                    if (!SetupDiCallClassInstaller(DIF_PROPERTYCHANGE, hDevInfo, ref di))
                                    {
                                        error = "CallClassInstaller failed (" + Marshal.GetLastWin32Error() + ")";
                                        return false;
                                    }
                                    return true;
                                }
                            }
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    idx++;
                    di = default(SP_DEVINFO_DATA);
                    di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                }

                error = "Device '" + friendlyName + "' not found in MEDIA class";
                return false;
            }
            finally { SetupDiDestroyDeviceInfoList(hDevInfo); }
        }

        public static bool IsDeviceEnabled(string friendlyName)
        {
            var guid = MediaClassGuid;
            var hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT);
            if (hDevInfo == IntPtr.Zero || hDevInfo == unchecked((IntPtr)(-1)))
                return true;

            try
            {
                var di = default(SP_DEVINFO_DATA);
                di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                uint idx = 0;

                while (SetupDiEnumDeviceInfo(hDevInfo, idx, ref di))
                {
                    uint sz = 0;
                    uint rdt = 0;
                    SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                        out rdt, IntPtr.Zero, 0, out sz);

                    if (sz > 0)
                    {
                        var buf = Marshal.AllocHGlobal((int)sz);
                        try
                        {
                            uint rdt2 = 0;
                            if (SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                                out rdt2, buf, sz, out sz))
                            {
                                var name = Marshal.PtrToStringUni(buf);
                                if (name != null && name.Equals(friendlyName, StringComparison.OrdinalIgnoreCase))
                                {
                                    uint status = 0;
                                    uint problem = 0;
                                    int cr = CM_Get_DevNode_Status(out status, out problem, di.DevInst, 0);
                                    return cr == 0 && (status & DN_STARTED) != 0;
                                }
                            }
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    idx++;
                    di = default(SP_DEVINFO_DATA);
                    di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                }
            }
            finally { SetupDiDestroyDeviceInfoList(hDevInfo); }
            return true;
        }

        public static List<string> EnumerateMediaDevices()
        {
            var result = new List<string>();
            var guid = MediaClassGuid;
            var hDevInfo = SetupDiGetClassDevs(ref guid, null, IntPtr.Zero, DIGCF_PRESENT);
            if (hDevInfo == IntPtr.Zero || hDevInfo == unchecked((IntPtr)(-1)))
                return result;
            try
            {
                var di = default(SP_DEVINFO_DATA);
                di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                uint idx = 0;
                while (SetupDiEnumDeviceInfo(hDevInfo, idx, ref di))
                {
                    uint sz = 0;
                    uint rdt = 0;
                    SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                        out rdt, IntPtr.Zero, 0, out sz);
                    if (sz > 0)
                    {
                        var buf = Marshal.AllocHGlobal((int)sz);
                        try
                        {
                            uint rdt2 = 0;
                            if (SetupDiGetDeviceRegistryProperty(hDevInfo, ref di, SPDRP_FRIENDLYNAME,
                                out rdt2, buf, sz, out sz))
                            {
                                var name = Marshal.PtrToStringUni(buf);
                                if (!string.IsNullOrEmpty(name))
                                    result.Add(name);
                            }
                        }
                        finally { Marshal.FreeHGlobal(buf); }
                    }
                    idx++;
                    di = default(SP_DEVINFO_DATA);
                    di.cbSize = (uint)Marshal.SizeOf(typeof(SP_DEVINFO_DATA));
                }
            }
            finally { SetupDiDestroyDeviceInfoList(hDevInfo); }
            return result;
        }
    }
}