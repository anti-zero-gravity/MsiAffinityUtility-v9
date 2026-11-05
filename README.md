[English](README.md) | [日本語](README.ja.md)

---

# MSI Affinity Utility v9

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
   - **Rollback / Backup Script**: Automatically generates a `.reg` script representing the exact original state before your changes, allowing instant restoration if needed. Preserves the initial launch state so you can always export a rollback script back to your starting settings.

4. **Modern UI & High-DPI Support**
   - Built with Windows Forms on .NET 8, fully DPI-aware (Per-Monitor V2).
   - Real-time status console with distinct color highlighting for Apply mode (Blue) and Rollback mode (Amber).

---

## UI Columns & Configurable Parameters

Summary of all columns displayed in the application grid, distinguishing between user-configurable parameters and read-only diagnostic info.

### User-Configurable Parameters (4 Items)

| Column Name | Input Type | Description & Purpose | Registry Target |
|---|:---:|---|---|
| **`msi`** | Checkbox | **MSI Mode Toggle**<br>・Checked: Message Signaled Interrupts (Low-latency mode)<br>・Unchecked: Legacy Line-based IRQ sharing | `MSISupported` (DWORD) |
| **`limit`** | Text Input | **Maximum MSI Message (Vector) Limit**<br>・Blank: Windows/Driver dynamically assigns optimal queue count<br>・`1`: Forces single-vector operation for dedicated core isolation | `MessageNumberLimit` (DWORD) |
| **`priority`** | Dropdown | **Interrupt Processing Priority**<br>・`Undefined` (Driver default)<br>・`Low` / `Normal` / `High` | `DevicePriority` (DWORD) |
| **`affinity`**<br>(P0, P1, E12...) | Core Checkboxes | **Target CPU Cores for Interrupt Servicing**<br>・All Clear: Dynamic OS scheduling across all available cores<br>・Selected: Restricts interrupt routing strictly to the chosen CPU core set | `DevicePolicy` = 4<br>`AssignmentSetOverride` (DWORD / QWORD) |

### Read-Only Diagnostic & Capability Information (6 Items)

| Column Name | Access | Description |
|---|:---:|---|
| **`device`** | Read-Only | Friendly device name and hardware identifier (PCI ID) |
| **`vendor`** | Read-Only | Hardware manufacturer name (NVIDIA, Intel, Realtek, Samsung, etc.) |
| **`type`** | Read-Only | Hardware category classification (GPU, Network, Storage, USB, Audio, etc.) |
| **`max limit`** | Read-Only | Theoretical maximum message vector count supported by the hardware capability |
| **`supported modes`** | Read-Only | Interrupt signaling capabilities supported by the device (LineBased, Msi, MsiX) |
| **`irq`** | Read-Only | Currently assigned hardware IRQ number in the active Windows session |

---

## Deep Dive: Limit, Max Limit, and Core Affinity Tuning

### 1. Understanding `limit` vs. `max limit`
- **`max limit` (Hardware Capability, Read-Only)**:
  - The maximum number of MSI/MSI-X messages (vectors) supported by the PCIe hardware and its specification.
  - Typical values: GPU / Audio / SATA: `1`, USB 3.x xHCI: `8`, 2.5GbE / 10GbE LAN: `32`, NVMe SSD: `33` (1 Admin + 32 I/O queues).
- **`limit` (Configured Value, `MessageNumberLimit`)**:
  - The registry parameter instructing Windows PnP on the maximum number of MSI messages to allocate to the device.
  - **Blank (Unset)**: Windows and the device driver automatically determine the optimal count (usually matching the number of hardware queues).
  - **Explicit Number (e.g., `1`)**: Constrains allocated message vectors to that exact ceiling.

### 2. Independence of Core Affinity and Limit
In the Windows PnP interrupt architecture, **"How many messages to allocate (`MessageNumberLimit`)"** and **"Which CPU cores should service those messages (`AssignmentSetOverride`)"** are strictly orthogonal and independent policies.

Restricting affinity to **a single CPU core does NOT cause Windows to automatically collapse the message count to 1**.

### 3. Recommended Tuning Strategies

| Tuning Goal | Recommended `limit` | Recommended `Affinity` | Architecture & Behavior |
|---|:---:|:---:|---|
| **Ultra-Low Latency & Exclusive Core**<br>(Competitive Gaming, Pro Audio) | **`1`** | **Single CPU Core** | Forces the device into single-vector mode. Eliminates DPC serialization spikes, queue bottlenecks, and vector saturation on the target core. |
| **High Throughput & Queue Parallelism**<br>(NVMe SSDs, 2.5GbE/10GbE Network) | **Blank**<br>(Unset) | **Multiple Cores** or **Single Core** | Preserves all multi-queue channels (e.g., 64 NVMe I/O queues or multi-channel RSS). If assigned to a single core, all completion vectors target that core without sacrificing queue depth. |

---

## Requirements

- **OS**: Windows 10 (1809+) or Windows 11 (64-bit)
- **Runtime**: [.NET 8.0 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/8.0)
- **Privilege**: Administrator rights (required for modifying device interrupt registry keys)

---

## Download

Pre-built binaries can be downloaded directly from the **[Releases](https://github.com/anti-zero-gravity/MsiAffinityUtility-v9/releases)** page:
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
