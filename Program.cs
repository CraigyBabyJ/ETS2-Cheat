using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

internal sealed class MainForm : Form
{
    private const string ToolVersion = "1.0.0";
    private const string GameVersion = "1.60";
    private const string DiscordUrl = "https://discord.craigybabyj.com";

    private static readonly Color WindowBack = Color.FromArgb(18, 22, 28);
    private static readonly Color PanelBack = Color.FromArgb(25, 31, 39);
    private static readonly Color ButtonBack = Color.FromArgb(42, 51, 63);
    private static readonly Color ButtonHover = Color.FromArgb(55, 66, 82);
    private static readonly Color ButtonFore = Color.FromArgb(238, 242, 247);
    private static readonly Color TextMuted = Color.FromArgb(165, 176, 190);
    private static readonly Color LogBack = Color.FromArgb(9, 12, 16);
    private static readonly Color Border = Color.FromArgb(64, 76, 92);

    private readonly TextBox _log = new();
    private readonly Label _status = new();
    private readonly Button _toggle = new();

    public MainForm()
    {
        Text = $"ETS2 Cheat v{ToolVersion}";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 640;
        Height = 462;
        MinimumSize = new Size(560, 340);
        BackColor = WindowBack;
        ForeColor = ButtonFore;
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "truck.ico");
        if (File.Exists(iconPath))
        {
            Icon = new Icon(iconPath);
        }

        var title = new Label
        {
            Text = $"ETS2 Cheat v{ToolVersion}",
            Dock = DockStyle.Top,
            Height = 38,
            Font = new Font(Font.FontFamily, 16, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(14, 0, 0, 0),
            BackColor = WindowBack,
            ForeColor = ButtonFore
        };

        var statusPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 152,
            Padding = new Padding(14, 10, 14, 10),
            BackColor = PanelBack
        };

        _status.Text = "Checking...";
        _status.Left = 16;
        _status.Top = 12;
        _status.Width = 360;
        _status.Height = 32;
        _status.Font = new Font(Font.FontFamily, 14, FontStyle.Bold);
        _status.ForeColor = TextMuted;
        _status.BackColor = PanelBack;

        _toggle.Text = "Enable";
        _toggle.Left = 16;
        _toggle.Top = 54;
        _toggle.Width = 150;
        _toggle.Height = 36;
        _toggle.Font = new Font(Font.FontFamily, 10, FontStyle.Bold);
        _toggle.Click += (_, _) => TogglePatch();

        var refresh = MakeButton("Refresh", () => RefreshStatus());
        refresh.Left = 178;
        refresh.Top = 54;

        var restore = MakeButton("Restore", () => RunAndRefresh("restore"));
        restore.Left = 296;
        restore.Top = 54;

        var reset = MakeButton("Reset Damage", () => RunAndRefresh("reset"));
        reset.Left = 414;
        reset.Top = 54;
        reset.Width = 130;

        var addMoney = MakeButton("+100,000 Money", () => RunAndRefresh("money"));
        addMoney.Left = 16;
        addMoney.Top = 98;
        addMoney.Width = 150;

        var removeMoney = MakeButton("-100,000 Money", () => RunAndRefresh("money-minus"));
        removeMoney.Left = 178;
        removeMoney.Top = 98;
        removeMoney.Width = 150;

        statusPanel.Controls.Add(_status);
        statusPanel.Controls.Add(_toggle);
        statusPanel.Controls.Add(refresh);
        statusPanel.Controls.Add(restore);
        statusPanel.Controls.Add(reset);
        statusPanel.Controls.Add(addMoney);
        statusPanel.Controls.Add(removeMoney);

        _log.Dock = DockStyle.Fill;
        _log.Multiline = true;
        _log.ReadOnly = true;
        _log.ScrollBars = ScrollBars.Vertical;
        _log.Font = new Font("Consolas", 10);
        _log.BackColor = LogBack;
        _log.ForeColor = Color.FromArgb(225, 232, 240);
        _log.BorderStyle = BorderStyle.FixedSingle;

        var discordIconPath = Path.Combine(AppContext.BaseDirectory, "Assets", "discord.ico");
        var footer = MakeFooter(discordIconPath);

        Controls.Add(_log);
        Controls.Add(footer);
        Controls.Add(statusPanel);
        Controls.Add(title);

        Shown += (_, _) => RefreshStatus();
    }

    private static Button MakeButton(string text, Action action)
    {
        var button = new Button
        {
            Text = text,
            Width = 110,
            Height = 36,
            Margin = new Padding(4),
            FlatStyle = FlatStyle.Flat,
            BackColor = ButtonBack,
            ForeColor = ButtonFore,
            UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = Border;
        button.FlatAppearance.MouseOverBackColor = ButtonHover;
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(34, 42, 53);
        button.Click += (_, _) => action();
        return button;
    }

    private static Panel MakeFooter(string iconPath)
    {
        var footer = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 38,
            Padding = new Padding(12, 5, 12, 5),
            BackColor = PanelBack
        };

        var version = new Label
        {
            Text = $"Tool v{ToolVersion}   |   ETS2 v{GameVersion}",
            AutoSize = false,
            Left = 14,
            Top = 9,
            Width = 260,
            Height = 22,
            ForeColor = TextMuted,
            BackColor = PanelBack,
            TextAlign = ContentAlignment.MiddleLeft
        };

        var discordIcon = new PictureBox
        {
            Left = 572,
            Top = 5,
            Width = 28,
            Height = 28,
            SizeMode = PictureBoxSizeMode.Zoom,
            Cursor = Cursors.Hand,
            BackColor = PanelBack
        };
        if (File.Exists(iconPath))
        {
            using var icon = new Icon(iconPath, 32, 32);
            discordIcon.Image = icon.ToBitmap();
        }

        void OpenDiscord()
        {
            Process.Start(new ProcessStartInfo(DiscordUrl) { UseShellExecute = true });
        }

        discordIcon.Click += (_, _) => OpenDiscord();

        footer.Controls.Add(version);
        footer.Controls.Add(discordIcon);
        return footer;
    }

    private void TogglePatch()
    {
        var state = Ets2DamagePatcher.GetState();
        RunAndRefresh(state == PatchState.Patched ? "restore" : "patch");
    }

    private void RunAndRefresh(string command)
    {
        try
        {
            _log.Text = Ets2DamagePatcher.Run(command);
            UpdateStatus(Ets2DamagePatcher.GetState());
        }
        catch (Exception ex)
        {
            _log.Text = ex.ToString();
            UpdateStatus(PatchState.Unknown);
        }
    }

    private void RefreshStatus()
    {
        try
        {
            var state = Ets2DamagePatcher.GetState();
            UpdateStatus(state);
            _log.Text = Ets2DamagePatcher.Run("status");
        }
        catch (Exception ex)
        {
            _status.Text = "ETS2 not ready";
            _status.ForeColor = Color.FromArgb(160, 70, 60);
            _toggle.Text = "Enable";
            _toggle.Enabled = false;
            _log.Text = ex.Message;
        }
    }

    private void UpdateStatus(PatchState state)
    {
        _toggle.Enabled = state != PatchState.NotRunning && state != PatchState.Unknown;

        if (state == PatchState.Patched)
        {
            _status.Text = "Damage stopper: ON";
            _status.ForeColor = Color.FromArgb(87, 210, 135);
            _toggle.Text = "Disable";
        }
        else if (state == PatchState.Original)
        {
            _status.Text = "Damage stopper: OFF";
            _status.ForeColor = Color.FromArgb(238, 178, 96);
            _toggle.Text = "Enable";
        }
        else if (state == PatchState.NotRunning)
        {
            _status.Text = "ETS2 not running";
            _status.ForeColor = Color.FromArgb(238, 108, 108);
            _toggle.Text = "Enable";
        }
        else
        {
            _status.Text = "Unknown patch state";
            _status.ForeColor = Color.FromArgb(238, 108, 108);
            _toggle.Text = "Enable";
        }
    }
}

internal static class Ets2DamagePatcher
{
    private const string ProcessName = "eurotrucks2";
    private const string ModuleName = "eurotrucks2.exe";

    private const int PROCESS_VM_OPERATION = 0x0008;
    private const int PROCESS_VM_READ = 0x0010;
    private const int PROCESS_VM_WRITE = 0x0020;
    private const int PROCESS_QUERY_INFORMATION = 0x0400;
    private const uint PAGE_EXECUTE_READWRITE = 0x40;

    private const int TruckManagerGlobalOffset = 0x33C0548;
    private const int CurrentTruckOffset = 0x2F98;
    private const int PrimaryDamageRootOffset = 0x18;
    private const int DamageNodeOffset = 0x1A8;
    private const int MoneyContainerStaticOffset = 0x2D4F118;
    private const int MoneyObjectOffset = 0x10;
    private const int MoneyValueOffset = 0x10;
    private const long MoneyIncrement = 100_000;

    private static readonly int[] SourceDamageFloatOffsets = { 0x80, 0x128, 0x160, 0x164, 0x168 };
    private static readonly int[] SourceWearFloatOffsets = { 0x84, 0x12C, 0x130, 0x140, 0x16C, 0x170, 0x174, 0x1C8, 0x254, 0x33C };

    private static readonly Patch[] Patches =
    {
        new("Main damage source clamp", 0x10C81F9, new byte[] { 0x44, 0x89, 0x44, 0x88, 0x04 }, new byte[] { 0x83, 0x64, 0x88, 0x04, 0x00 }),
        new("Mirror copy clamp A", 0x10C7A10, new byte[] { 0x41, 0x8B, 0x41, 0x04 }, new byte[] { 0x31, 0xC0, 0x66, 0x90 }),
        new("Mirror copy clamp B", 0x10C7B4A, new byte[] { 0xF3, 0x48, 0x0F, 0x2C, 0xC2 }, new byte[] { 0x31, 0xC0, 0x0F, 0x1F, 0x00 }),
        new("Display mirror clamp", 0x10C7600, new byte[] { 0x8B, 0x41, 0x04 }, new byte[] { 0x31, 0xC0, 0x90 }),
        new("Crash update mirror clamp", 0x8FC9B5, new byte[] { 0x44, 0x89, 0x38 }, new byte[] { 0x89, 0x10, 0x90 }),
    };

    public static string Run(string command)
    {
        var log = new StringBuilder();
        var target = OpenTarget();
        if (target is null) return "ETS2 is not running. Start the game and load your save first.";

        using var handle = target.Handle;
        var process = target.Process;
        var module = target.Module;

        log.AppendLine($"Attached to {process.ProcessName}.exe PID {process.Id}");
        log.AppendLine($"Module base: 0x{module.BaseAddress.ToInt64():X}");
        log.AppendLine();

        if (command == "reset")
        {
            ResetDamage(handle, module, log);
            return log.ToString();
        }

        if (command == "money")
        {
            ChangeMoney(handle, module, MoneyIncrement, log);
            return log.ToString();
        }

        if (command == "money-minus")
        {
            ChangeMoney(handle, module, -MoneyIncrement, log);
            return log.ToString();
        }

        foreach (var patch in Patches)
        {
            var address = module.BaseAddress + patch.Offset;
            var current = ReadBytes(handle, address, patch.Original.Length);
            var targetBytes = command == "restore" ? patch.Original : patch.Patched;

            if (command == "status")
            {
                var state = BytesEqual(current, patch.Patched) ? "patched" :
                    BytesEqual(current, patch.Original) ? "original" : "unknown";
                log.AppendLine($"{patch.Name}: {state} at +0x{patch.Offset:X} [{ToHex(current)}]");
                continue;
            }

            if (!BytesEqual(current, patch.Original) && !BytesEqual(current, patch.Patched))
            {
                log.AppendLine($"{patch.Name}: skipped unknown bytes at +0x{patch.Offset:X}. Current [{ToHex(current)}]");
                continue;
            }

            WriteBytes(handle, address, targetBytes);
            log.AppendLine($"{patch.Name}: {(command == "restore" ? "restored" : "patched")} at +0x{patch.Offset:X}");
        }

        log.AppendLine();
        log.AppendLine(command == "restore"
            ? "Original bytes restored."
            : command == "status"
                ? "Status complete."
                : "Patch complete. If damage was already above 0, repair once, then new damage should stay at 0.");
        return log.ToString();
    }

    private static void ResetDamage(SafeProcessHandle handle, ProcessModule module, StringBuilder log)
    {
        var manager = ReadPointer(handle, module.BaseAddress + TruckManagerGlobalOffset);
        if (manager == IntPtr.Zero)
        {
            log.AppendLine("Could not find ETS2 truck manager.");
            return;
        }

        var truck = ReadPointer(handle, manager + CurrentTruckOffset);
        if (truck == IntPtr.Zero)
        {
            log.AppendLine("Could not find current truck object. Load into the truck first.");
            return;
        }

        var damageRoot = ReadPointer(handle, truck + PrimaryDamageRootOffset);
        if (damageRoot == IntPtr.Zero)
        {
            log.AppendLine("Current truck has no primary damage root.");
            return;
        }

        var damageNode = ReadPointer(handle, damageRoot + DamageNodeOffset);
        if (damageNode == IntPtr.Zero)
        {
            log.AppendLine("Current truck has no damage node.");
            return;
        }

        log.AppendLine($"Truck object: 0x{truck.ToInt64():X}");
        log.AppendLine($"Damage node:  0x{damageNode.ToInt64():X}");
        log.AppendLine();

        foreach (var offset in SourceDamageFloatOffsets)
        {
            var address = damageNode + offset;
            var before = ReadFloat(handle, address);
            WriteFloat(handle, address, 0f);
            var after = ReadFloat(handle, address);
            log.AppendLine($"damage +0x{offset:X}: {before:0.000000} -> {after:0.000000}");
        }

        foreach (var offset in SourceWearFloatOffsets)
        {
            var address = damageNode + offset;
            var before = ReadFloat(handle, address);
            WriteFloat(handle, address, 0f);
            var after = ReadFloat(handle, address);
            log.AppendLine($"wear   +0x{offset:X}: {before:0.000000} -> {after:0.000000}");
        }

        log.AppendLine();
        log.AppendLine("Source damage/wear reset. Switch screens or reopen the damage report if the UI has not refreshed yet.");
    }

    private static void ChangeMoney(SafeProcessHandle handle, ProcessModule module, long delta, StringBuilder log)
    {
        var container = ReadPointer(handle, module.BaseAddress + MoneyContainerStaticOffset);
        if (container == IntPtr.Zero)
        {
            log.AppendLine("Could not find ETS2 money container.");
            return;
        }

        var moneyObject = ReadPointer(handle, container + MoneyObjectOffset);
        if (moneyObject == IntPtr.Zero)
        {
            log.AppendLine("Could not find ETS2 money object.");
            return;
        }

        var moneyAddress = moneyObject + MoneyValueOffset;
        var before = ReadInt64(handle, moneyAddress);
        var after = checked(before + delta);
        WriteInt64(handle, moneyAddress, after);

        log.AppendLine($"Money container: 0x{container.ToInt64():X}");
        log.AppendLine($"Money object:    0x{moneyObject.ToInt64():X}");
        log.AppendLine($"Money address:   0x{moneyAddress.ToInt64():X}");
        log.AppendLine();
        log.AppendLine($"Money: {before:N0} -> {after:N0} ({delta:+#,0;-#,0;0})");
    }

    public static PatchState GetState()
    {
        var target = OpenTarget();
        if (target is null) return PatchState.NotRunning;

        using var handle = target.Handle;
        var original = 0;
        var patched = 0;

        foreach (var patch in Patches)
        {
            var current = ReadBytes(handle, target.Module.BaseAddress + patch.Offset, patch.Original.Length);
            if (BytesEqual(current, patch.Original)) original++;
            else if (BytesEqual(current, patch.Patched)) patched++;
            else return PatchState.Unknown;
        }

        if (patched == Patches.Length) return PatchState.Patched;
        if (original == Patches.Length) return PatchState.Original;
        return PatchState.Unknown;
    }

    private static Target? OpenTarget()
    {
        var process = Process.GetProcessesByName(ProcessName).FirstOrDefault();
        if (process is null) return null;

        var module = process.Modules.Cast<ProcessModule>()
            .FirstOrDefault(m => string.Equals(m.ModuleName, ModuleName, StringComparison.OrdinalIgnoreCase));
        if (module is null) return null;

        var handle = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_OPERATION | PROCESS_VM_READ | PROCESS_VM_WRITE, false, process.Id);
        if (handle.IsInvalid)
        {
            FailLastWin32("OpenProcess failed");
        }

        return new Target(process, module, handle);
    }

    private static byte[] ReadBytes(SafeProcessHandle handle, IntPtr address, int length)
    {
        var buffer = new byte[length];
        if (!ReadProcessMemory(handle, address, buffer, buffer.Length, out var read) || read != length)
        {
            FailLastWin32($"ReadProcessMemory failed at 0x{address.ToInt64():X}");
        }

        return buffer;
    }

    private static IntPtr ReadPointer(SafeProcessHandle handle, IntPtr address)
    {
        var bytes = ReadBytes(handle, address, IntPtr.Size);
        return new IntPtr(BitConverter.ToInt64(bytes, 0));
    }

    private static float ReadFloat(SafeProcessHandle handle, IntPtr address)
    {
        return BitConverter.ToSingle(ReadBytes(handle, address, sizeof(float)), 0);
    }

    private static void WriteFloat(SafeProcessHandle handle, IntPtr address, float value)
    {
        WriteBytes(handle, address, BitConverter.GetBytes(value));
    }

    private static long ReadInt64(SafeProcessHandle handle, IntPtr address)
    {
        return BitConverter.ToInt64(ReadBytes(handle, address, sizeof(long)), 0);
    }

    private static void WriteInt64(SafeProcessHandle handle, IntPtr address, long value)
    {
        WriteBytes(handle, address, BitConverter.GetBytes(value));
    }

    private static void WriteBytes(SafeProcessHandle handle, IntPtr address, byte[] bytes)
    {
        if (!VirtualProtectEx(handle, address, (UIntPtr)bytes.Length, PAGE_EXECUTE_READWRITE, out var oldProtect))
        {
            FailLastWin32($"VirtualProtectEx failed at 0x{address.ToInt64():X}");
        }

        if (!WriteProcessMemory(handle, address, bytes, bytes.Length, out var written) || written != bytes.Length)
        {
            FailLastWin32($"WriteProcessMemory failed at 0x{address.ToInt64():X}");
        }

        FlushInstructionCache(handle, address, (UIntPtr)bytes.Length);
        VirtualProtectEx(handle, address, (UIntPtr)bytes.Length, oldProtect, out _);
    }

    private static bool BytesEqual(byte[] left, byte[] right) => left.SequenceEqual(right);

    private static string ToHex(byte[] bytes) => string.Join(' ', bytes.Select(b => b.ToString("X2")));

    private static void FailLastWin32(string message)
    {
        throw new Win32Exception(Marshal.GetLastWin32Error(), message);
    }

    private sealed record Patch(string Name, int Offset, byte[] Original, byte[] Patched);

    private sealed record Target(Process Process, ProcessModule Module, SafeProcessHandle Handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern SafeProcessHandle OpenProcess(int desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteProcessMemory(SafeProcessHandle hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesWritten);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool VirtualProtectEx(SafeProcessHandle hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushInstructionCache(SafeProcessHandle hProcess, IntPtr lpBaseAddress, UIntPtr dwSize);
}

internal enum PatchState
{
    NotRunning,
    Original,
    Patched,
    Unknown
}

internal sealed class SafeProcessHandle : SafeHandle
{
    public SafeProcessHandle() : base(IntPtr.Zero, true)
    {
    }

    public override bool IsInvalid => handle == IntPtr.Zero || handle == new IntPtr(-1);

    protected override bool ReleaseHandle() => CloseHandle(handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);
}
