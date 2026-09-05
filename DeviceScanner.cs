using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace MsiAffinityUtility
{
    public class PciDeviceInfo
    {
        public string InstancePath { get; set; } = "";
        public string Name { get; set; } = "";
        public string Vendor { get; set; } = "";
        public string Irq { get; set; } = "";
        public bool Msi { get; set; }
        public string Limit { get; set; } = "";
        public string MaxLimit { get; set; } = "";
        public string SupportedModes { get; set; } = "";
        public string Priority { get; set; } = "Undefined";
        public ulong AffinityMask { get; set; }
        public bool HasAffinityPolicy { get; set; }

        // 変更差分検出用の初期状態バックアップ
        public bool OrigMsi { get; set; }
        public string OrigLimit { get; set; } = "";
        public string OrigPriority { get; set; } = "Undefined";
        public ulong OrigAffinityMask { get; set; }
        public bool OrigHasAffinityPolicy { get; set; }

        public string RegistryPath => $@"SYSTEM\CurrentControlSet\Enum\{InstancePath}";
    }

    public static class DeviceScanner
    {
        public static List<PciDeviceInfo> ScanPciDevices()
        {
            var results = new List<PciDeviceInfo>();

            using var pciKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\PCI");
            if (pciKey == null) return results;

            foreach (var hardwareId in pciKey.GetSubKeyNames())
            {
                using var hwKey = pciKey.OpenSubKey(hardwareId);
                if (hwKey == null) continue;

                foreach (var instanceId in hwKey.GetSubKeyNames())
                {
                    string fullPath = $@"PCI\{hardwareId}\{instanceId}";
                    using var devKey = hwKey.OpenSubKey(instanceId);
                    if (devKey == null) continue;

                    string desc = devKey.GetValue("DeviceDesc") as string ?? "";
                    string friendly = devKey.GetValue("FriendlyName") as string ?? "";
                    string name = !string.IsNullOrEmpty(desc) ? desc : (!string.IsNullOrEmpty(friendly) ? friendly : hardwareId);

                    string vendor = VendorLookup.GetVendor(fullPath);

                    string msiSubKey = $@"SYSTEM\CurrentControlSet\Enum\{fullPath}\Device Parameters\Interrupt Management\MessageSignaledInterruptProperties";
                    string affSubKey = $@"SYSTEM\CurrentControlSet\Enum\{fullPath}\Device Parameters\Interrupt Management\Affinity Policy";
                    string routeSubKey = $@"SYSTEM\CurrentControlSet\Enum\{fullPath}\Device Parameters\Interrupt Management\Routing Info";

                    bool msiSupported = false;
                    string limit = "";
                    string maxLimit = "";
                    string priority = "Undefined";
                    ulong affinityMask = 0;
                    bool hasAffinity = false;
                    string irq = "";

                    using var msiKey = Registry.LocalMachine.OpenSubKey(msiSubKey);
                    if (msiKey != null)
                    {
                        object? msiVal = msiKey.GetValue("MSISupported");
                        if (msiVal != null)
                        {
                            msiSupported = (Convert.ToInt32(msiVal) == 1);
                        }

                        object? limVal = msiKey.GetValue("MessageNumberLimit");
                        if (limVal != null)
                        {
                            limit = limVal.ToString() ?? "";
                        }
                    }

                    using var affKey = Registry.LocalMachine.OpenSubKey(affSubKey);
                    if (affKey != null)
                    {
                        object? priVal = affKey.GetValue("DevicePriority");
                        if (priVal != null)
                        {
                            priority = Convert.ToInt32(priVal) switch
                            {
                                1 => "Low",
                                2 => "Normal",
                                3 => "High",
                                _ => "Undefined"
                            };
                        }

                        object? polVal = affKey.GetValue("DevicePolicy");
                        object? setVal = affKey.GetValue("AssignmentSetOverride");
                        if (polVal != null && Convert.ToInt32(polVal) == 4 && setVal != null)
                        {
                            hasAffinity = true;
                            affinityMask = Convert.ToUInt64(setVal);
                        }
                    }

                    using var routeKey = Registry.LocalMachine.OpenSubKey(routeSubKey);
                    if (routeKey != null)
                    {
                        object? vecVal = routeKey.GetValue("StaticVector");
                        if (vecVal != null)
                        {
                            int vec = Convert.ToInt32(vecVal);
                            if (vec > 0)
                            {
                                irq = $"-{vec - 1}";
                            }
                        }
                    }

                    string supportedModes = DetermineSupportedModes(name, fullPath, msiKey != null, ref maxLimit);

                    if (string.IsNullOrEmpty(irq))
                    {
                        irq = msiSupported ? "-32" : "16";
                    }

                    results.Add(new PciDeviceInfo
                    {
                        InstancePath = fullPath,
                        Name = name,
                        Vendor = vendor,
                        Irq = irq,
                        Msi = msiSupported,
                        Limit = limit,
                        MaxLimit = maxLimit,
                        SupportedModes = supportedModes,
                        Priority = priority,
                        AffinityMask = affinityMask,
                        HasAffinityPolicy = hasAffinity,

                        OrigMsi = msiSupported,
                        OrigLimit = limit,
                        OrigPriority = priority,
                        OrigAffinityMask = affinityMask,
                        OrigHasAffinityPolicy = hasAffinity
                    });
                }
            }

            return results;
        }

        private static string DetermineSupportedModes(string name, string path, bool hasMsiKey, ref string maxLimit)
        {
            string upperName = name.ToUpperInvariant();
            string upperPath = path.ToUpperInvariant();

            if (upperName.Contains("NVM EXPRESS") || upperName.Contains("NVME"))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "33";
                return "LineBased, Msi, MsiX";
            }

            if (upperName.Contains("2.5GBE") || upperName.Contains("10GBE") || upperName.Contains("I210") || upperName.Contains("I218") || upperName.Contains("KILLER"))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "32";
                return "LineBased, Msi, MsiX";
            }

            if (upperName.Contains("GEFORCE") || upperName.Contains("RADEON") || upperName.Contains("NVIDIA") || upperPath.Contains("VEN_10DE") || upperPath.Contains("VEN_1002"))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "1";
                return "LineBased, Msi";
            }

            if (upperName.Contains("XHCI") || upperName.Contains("USB 3."))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "8";
                return "LineBased, Msi, MsiX";
            }

            if (upperName.Contains("AHCI") || upperName.Contains("SATA"))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "1";
                return "LineBased, Msi";
            }

            if (upperName.Contains("HIGH DEFINITION AUDIO") || upperName.Contains("AUDIO CONTROLLER"))
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "1";
                return "LineBased, Msi";
            }

            if (upperName.Contains("ENHANCED HOST CONTROLLER") || upperName.Contains("EHCI"))
            {
                maxLimit = "";
                return "LineBased";
            }

            if (hasMsiKey)
            {
                if (string.IsNullOrEmpty(maxLimit)) maxLimit = "1";
                return "LineBased, Msi";
            }

            return "LineBased";
        }
    }
}
