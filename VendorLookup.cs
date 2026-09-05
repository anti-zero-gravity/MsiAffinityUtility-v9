using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MsiAffinityUtility
{
    public static class VendorLookup
    {
        private static readonly Dictionary<string, string> Vendors = new(StringComparer.OrdinalIgnoreCase)
        {
            // 主要 GPU / チップセット / CPU
            { "10DE", "NVIDIA" },
            { "1002", "AMD" },
            { "1022", "AMD" },
            { "8086", "Intel" },

            // 主要マザーボード / ボードパートナー (SUBSYS)
            { "1043", "ASUS" },
            { "1462", "MSI" },
            { "1458", "GIGABYTE" },
            { "1849", "ASRock" },
            { "3842", "EVGA" },
            { "19DA", "ZOTAC" },
            { "1569", "Palit" },
            { "1DA2", "Sapphire" },
            { "148C", "PowerColor" },
            { "1787", "HIS" },
            { "1682", "XFX" },
            { "10B0", "Gainward" },
            { "7377", "Colorful" },
            { "152D", "Quanta" },
            { "1028", "Dell" },
            { "103C", "HP" },
            { "17AA", "Lenovo" },
            { "1179", "Toshiba" },
            { "10CF", "Fujitsu" },
            { "104D", "Sony" },
            { "144D", "Samsung" },

            // ネットワーク (LAN / Wi-Fi)
            { "10EC", "Realtek" },
            { "14E4", "Broadcom" },
            { "168C", "Qualcomm Atheros" },
            { "1D6A", "Aquantia / Marvell" },
            { "11AB", "Marvell" },
            { "1969", "Qualcomm Killer" },
            { "177D", "Cavium" },
            { "19A2", "Emulex" },
            { "1077", "QLogic" },
            { "13D3", "AzureWave" },

            // USB / PCI ブリッジ / コントローラ
            { "1B21", "ASMedia" },
            { "1033", "NEC / Renesas" },
            { "1912", "Renesas" },
            { "1B73", "Fresco Logic" },
            { "1106", "VIA Technologies" },
            { "10B5", "PLX Technology" },
            { "12D8", "Pericom" },
            { "104C", "Texas Instruments" },
            { "1057", "Motorola" },
            { "111D", "IDT" },
            { "1A03", "ASPEED" },

            // ストレージ (NVMe / SATA / RAID)
            { "15B7", "Western Digital" },
            { "1E0F", "KIOXIA" },
            { "1344", "Micron / Crucial" },
            { "1C5C", "SK Hynix" },
            { "1F4B", "SK Hynix" },
            { "1987", "Phison" },
            { "126F", "Silicon Motion" },
            { "1000", "LSI / Broadcom" },
            { "105A", "Promise" },
            { "1095", "Silicon Image" },
            { "13FE", "Kingston" },
            { "1B4B", "Marvell NVMe" },

            // オーディオ / メディア / キャプチャ
            { "1102", "Creative Labs" },
            { "13F6", "C-Media" },
            { "4163", "EARTH SOFT" },
            { "1822", "Twinhan" },
            { "127A", "Rockwell" },
            { "14F1", "Conexant" },
            { "1131", "Philips" },
            { "11C1", "Agere / LSI" },

            // 主要 USB 周辺 / オーディオブランド
            { "046D", "Logitech" },
            { "1B1C", "Corsair" },
            { "1532", "Razer" },
            { "1038", "SteelSeries" },
            { "2497", "RME Audio" },
            { "2972", "Topping" },
            { "07FD", "MOTU" },
            { "0B0E", "Jabra" },
            { "1395", "Sennheiser" },
            { "0D8C", "C-Media USB" },
            { "04D8", "Microchip" },
            { "16C0", "Teensy" },
            { "0483", "STMicroelectronics" }
        };

        public static string GetVendor(string deviceId)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
                return "-";

            // VEN_xxxx を抽出
            var venMatch = Regex.Match(deviceId, @"VEN_([0-9a-fA-F]{4})", RegexOptions.IgnoreCase);
            // SUBSYS_xxxxYYYY の YYYY (Subsystem Vendor) を抽出
            var subMatch = Regex.Match(deviceId, @"SUBSYS_[0-9a-fA-F]{4}([0-9a-fA-F]{4})", RegexOptions.IgnoreCase);

            string? chipVen = venMatch.Success ? venMatch.Groups[1].Value.ToUpperInvariant() : null;
            string? subVen = subMatch.Success ? subMatch.Groups[1].Value.ToUpperInvariant() : null;

            string chipName = (chipVen != null && Vendors.TryGetValue(chipVen, out var cName)) ? cName : (chipVen ?? "");
            string subName = (subVen != null && Vendors.TryGetValue(subVen, out var sName)) ? sName : "";

            // チップとボードメーカーの両方がある場合
            if (!string.IsNullOrEmpty(subName) && !string.IsNullOrEmpty(chipName))
            {
                if (!chipName.Equals(subName, StringComparison.OrdinalIgnoreCase))
                {
                    return $"{chipName} ({subName})";
                }
                return chipName;
            }

            if (!string.IsNullOrEmpty(chipName))
                return chipName;

            if (!string.IsNullOrEmpty(subName))
                return subName;

            return chipVen != null ? $"0x{chipVen}" : "-";
        }
    }
}
