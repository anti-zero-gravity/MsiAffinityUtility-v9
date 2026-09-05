[English](README.md) | [日本語](README.ja.md)

---

# MSI Affinity Utility (v9.0)

**MSI Affinity Utility** is an advanced Windows hardware interrupt configuration tool.  
It extends the classic MSI (Message Signaled-based Interrupts) utility concept by adding fine-grained **CPU Core Affinity management** (including Intel P-core / E-core hybrid architecture awareness) and instant registry **backup & rollback** generation.

Inspired by the classic community tool on Guru3D and [Sathango/Msi-Utility-v3](https://github.com/Sathango/Msi-Utility-v3), this project was redesigned and rewritten in modern **C# / .NET 8**.

---

## Key Features

1. **MSI Mode Toggle & IRQ Configuration**
   - Easily enable or disable Message Signaled Interrupts (MSI) for PCI/PCIe devices (GPU, Network, NVMe, USB controllers, etc.).
   - Adjust interrupt limit and priority settings.

2. **CPU Affinity Mask Assignment (Hybrid Architecture Aware)**
   - Target specific CPU cores for hardware interrupts to minimize input lag, frame drops, and latency spikes.
   - Automatically detects CPU topology and distinguishes **P-cores (Performance)** and **E-cores (Efficiency)** on modern Intel processors.
   - Core labels like `P0`, `P1`, `E12` or `E0`, `E1` (for all-E-core processors such as N100/N95) for intuitive assignment.

3. **Safe Apply & One-Click Rollback Script**
   - **Apply Changes**: Apply settings directly to the Windows registry with administrator privileges.
   - **Rollback / Backup Script**: Automatically generates a `.reg` script representing the exact original state before your changes, allowing instant restoration if needed.

4. **Modern UI & High-DPI Support**
   - Built with Windows Forms on .NET 8, fully DPI-aware (Per-Monitor V2).
   - Real-time status console with distinct color highlighting for Apply mode (Blue) and Rollback mode (Amber).

---

## Requirements

- **OS**: Windows 10 (1809+) or Windows 11 (64-bit)
- **Runtime**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Privilege**: Administrator rights (required for modifying device interrupt registry keys)

---

## Download

Pre-built binaries can be downloaded directly from the **[Releases](../../releases)** page:
- Download `MsiAffinityUtility_v9.exe` (Single-file executable)
- Run as Administrator

---

## How to Build from Source

You can build the single-file executable using the .NET 8 SDK:

### Prerequisites
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (v8.0.400 or later)

### Build Command
```powershell
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o ./publish
```

The output executable will be placed in `./publish/MsiAffinityUtility.exe`.

---

## Important Notice & Disclaimer

> [!CAUTION]
> **Use at Your Own Risk**  
> Modifying interrupt settings (MSI mode / Affinity) for essential system devices (such as ACPI controllers, chipset bridges, or system timers) can result in system instability or Blue Screen of Death (BSOD).  
> It is primarily recommended for **GPU (Graphics Cards)** and **Network Adapters (NIC)**.  
> The author assumes no responsibility for any system damages, data loss, or malfunctions caused by using this software. Always test changes cautiously and keep the generated rollback `.reg` script handy.

> [!NOTE]
> **False Positives**: Because this utility modifies low-level system registry settings, some antivirus programs or Windows SmartScreen may trigger false positive warnings. The complete source code is provided in this repository for full transparency.

---

## Credits & Acknowledgements

- Based on the concept and reverse-engineering of the community **MSI Utility** from the [Guru3D Forums](https://forums.guru3d.com/).
- Special thanks to [Sathango/Msi-Utility-v3](https://github.com/Sathango/Msi-Utility-v3) for keeping the utility accessible to the PC gaming and latency optimization community.

---

## License

This project is open-source under the [MIT License](LICENSE).
