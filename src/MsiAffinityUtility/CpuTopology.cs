using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MsiAffinityUtility
{
    public class CoreInfo
    {
        public int LogicalIndex { get; set; }
        public byte EfficiencyClass { get; set; }
        public string Label { get; set; } = "";
    }

    public static class CpuTopology
    {
        private const int RelationProcessorCore = 0;
        private const ushort ALL_PROCESSOR_GROUPS = 0xFFFF;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(
            int RelationshipType,
            IntPtr Buffer,
            ref uint ReturnedLength
        );

        [DllImport("kernel32.dll")]
        private static extern uint GetActiveProcessorCount(ushort GroupNumber);

        public static List<CoreInfo> GetCpuCores()
        {
            var list = new List<CoreInfo>();

            // プロセスのアフィニティ制限を無視して、システム全体の真の論理プロセッサ数を取得
            int totalLogical = (int)GetActiveProcessorCount(ALL_PROCESSOR_GROUPS);
            if (totalLogical <= 0)
            {
                totalLogical = Environment.ProcessorCount;
            }

            // CPU名から純EコアCPU（Intel N100, N95, N200, N300, N305, Atom等）を判定
            bool isAllECores = CheckIfAllECoresCpu();

            uint length = 0;
            GetLogicalProcessorInformationEx(RelationProcessorCore, IntPtr.Zero, ref length);
            if (length == 0)
            {
                string prefix = isAllECores ? "E" : "#";
                for (int i = 0; i < totalLogical; i++)
                {
                    list.Add(new CoreInfo { LogicalIndex = i, EfficiencyClass = 0, Label = $"{prefix}{i}" });
                }
                return list;
            }

            IntPtr buffer = Marshal.AllocHGlobal((int)length);
            try
            {
                if (GetLogicalProcessorInformationEx(RelationProcessorCore, buffer, ref length))
                {
                    IntPtr current = buffer;
                    int offset = 0;
                    var coreData = new List<(byte effClass, ulong mask)>();

                    while (offset < length)
                    {
                        int relationship = Marshal.ReadInt32(current, 0);
                        int size = Marshal.ReadInt32(current, 4);

                        if (relationship == RelationProcessorCore)
                        {
                            byte effClass = Marshal.ReadByte(current, 9);
                            ulong mask = (ulong)Marshal.ReadInt64(current, 32);
                            coreData.Add((effClass, mask));
                        }

                        offset += size;
                        current = new IntPtr(current.ToInt64() + size);
                    }

                    var effMap = new Dictionary<int, byte>();
                    int maxBitFound = -1;
                    foreach (var (effClass, mask) in coreData)
                    {
                        for (int bit = 0; bit < 64; bit++)
                        {
                            if ((mask & (1UL << bit)) != 0)
                            {
                                effMap[bit] = effClass;
                                if (bit > maxBitFound) maxBitFound = bit;
                            }
                        }
                    }

                    int coreCount = Math.Max(totalLogical, maxBitFound + 1);

                    var distinctClasses = effMap.Values.Distinct().ToList();
                    bool isHybrid = distinctClasses.Count > 1;
                    byte maxEff = distinctClasses.Count > 0 ? distinctClasses.Max() : (byte)0;

                    for (int i = 0; i < coreCount; i++)
                    {
                        byte eff = effMap.TryGetValue(i, out var e) ? e : (byte)0;
                        string label;
                        if (isHybrid)
                        {
                            // ハイブリッドCPU (12th/13th/14th Gen, Core Ultra等): P0, P1... / E16, E17...
                            label = (eff == maxEff) ? $"P{i}" : $"E{i}";
                        }
                        else if (isAllECores)
                        {
                            // 純EコアCPU (Intel N100, N95, N200, N300, N305等): E0, E1, E2, E3...
                            label = $"E{i}";
                        }
                        else
                        {
                            // 通常の均一CPU (Xeon, Ryzen, 従来型Core等): #0, #1, #2...
                            label = $"#{i}";
                        }

                        list.Add(new CoreInfo
                        {
                            LogicalIndex = i,
                            EfficiencyClass = eff,
                            Label = label
                        });
                    }
                }
            }
            catch
            {
                list.Clear();
                string prefix = isAllECores ? "E" : "#";
                for (int i = 0; i < totalLogical; i++)
                {
                    list.Add(new CoreInfo { LogicalIndex = i, EfficiencyClass = 0, Label = $"{prefix}{i}" });
                }
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            if (list.Count == 0)
            {
                string prefix = isAllECores ? "E" : "#";
                for (int i = 0; i < totalLogical; i++)
                {
                    list.Add(new CoreInfo { LogicalIndex = i, EfficiencyClass = 0, Label = $"{prefix}{i}" });
                }
            }

            return list;
        }

        private static bool CheckIfAllECoresCpu()
        {
            try
            {
                using var key = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DESCRIPTION\System\CentralProcessor\0");
                if (key != null)
                {
                    string name = key.GetValue("ProcessorNameString") as string ?? "";
                    if (!string.IsNullOrEmpty(name))
                    {
                        // Intel Alder Lake-N / Twin Lake / Atom 系 (Eコアのみで構成されたCPU)
                        // 例: "Intel(R) Processor N100", "Intel(R) Processor N95", "Intel(R) Core(TM) i3-N300", "N305" 等
                        if (Regex.IsMatch(name, @"\b(N100|N95|N97|N200|N50|N300|N305|N150|N250)\b", RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(name, @"Processor\s+N\d+", RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(name, @"i3-N\d+", RegexOptions.IgnoreCase))
                        {
                            return true;
                        }
                    }
                }
            }
            catch { }
            return false;
        }
    }
}
