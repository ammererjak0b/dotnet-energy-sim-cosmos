using System.Globalization;
using EnergySim.Core.Devices;
using EnergySim.Modbus;
using Microsoft.Extensions.Configuration;

namespace EnergySim.Diagnostic;

/// <summary>Menu loop: reads commands from stdin, talks to one device through the gateway, prints readable results.</summary>
public static class DiagnosticRunner
{
    private const int EXIT_OK = 0;
    private const int EXIT_ERROR = 1;
    private const int EXIT_USAGE = 2;
    private const int WATCH_INTERVAL_MS = 1000;
    private const int KEY_POLL_MS = 50;
    private const int WRITE_EFFECT_DELAY_MS = 500; // device applies a write on its next tick (200 ms)
    private const double LIMIT_PERCENT_MIN = 0;
    private const double LIMIT_PERCENT_MAX = 100;
    private const int PERMILLE_PER_PERCENT = 10;

    private const string USAGE = "usage: dotnet run --project src/EnergySim.Diagnostic -- [--Host <host>] [--Port <port>] [--UnitId <0..255>]";

    private const string COMMANDS = "commands: header | device | write on|off | write limit <0..100> | write reset | watch | target [name|number] | quit";

    public static async Task<int> runAsync(string[] args, CancellationToken ct)
    {
        try
        {
            if (Array.IndexOf(args, "--help") >= 0)
            {
                await Console.Out.WriteLineAsync(USAGE);
                return EXIT_OK;
            }

            DiagnosticSettings.checkArguments(args);
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", true)
                .AddCommandLine(args)
                .Build();
            DiagnosticSettings settings = DiagnosticSettings.load(configuration);
            return await menuLoopAsync(settings, ct);
        }
        catch (DiagnosticUsageException exception)
        {
            await Console.Error.WriteLineAsync("error: " + exception.Message);
            await Console.Error.WriteLineAsync(USAGE);
            return EXIT_USAGE;
        }
        catch (OperationCanceledException)
        {
            return EXIT_OK; // Ctrl+C
        }
    }

    private static async Task<int> menuLoopAsync(DiagnosticSettings settings, CancellationToken ct)
    {
        ModbusGateway gateway = new ModbusGateway(settings.StartTarget, settings.UnitId);
        try
        {
            await Console.Out.WriteLineAsync("target " + describe(gateway.Target));
            await Console.Out.WriteLineAsync(COMMANDS);
            while (true)
            {
                ct.ThrowIfCancellationRequested();
                await Console.Out.WriteAsync("> ");
                string? line = await readLineAsync(ct);
                if (line == null)
                {
                    return EXIT_OK; // end of input
                }

                string[] words = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (words.Length == 0)
                {
                    continue;
                }

                if (words[0].ToLowerInvariant() == "quit")
                {
                    return EXIT_OK;
                }

                if (words[0].ToLowerInvariant() == "target")
                {
                    gateway = await switchTargetAsync(settings, gateway, words, ct);
                    continue;
                }

                try
                {
                    await runCommandAsync(gateway, words, ct);
                }
                catch (DiagnosticException exception)
                {
                    await Console.Out.WriteLineAsync("error: " + exception.Message);
                    if (exception.IsFatal)
                    {
                        return EXIT_ERROR;
                    }
                }
            }
        }
        finally
        {
            gateway.Dispose();
        }
    }

    private static async Task runCommandAsync(ModbusGateway gateway, string[] words, CancellationToken ct)
    {
        string command = words[0].ToLowerInvariant();
        if (command == "header")
        {
            HeaderSnapshot header = await readHeaderAsync(gateway, ct);
            await printHeaderAsync(header);
            return;
        }

        if (command == "device")
        {
            await deviceAsync(gateway, ct);
            return;
        }

        if (command == "write")
        {
            await writeAsync(gateway, words, ct);
            return;
        }

        if (command == "watch")
        {
            await watchAsync(gateway, ct);
            return;
        }

        await Console.Out.WriteLineAsync("unknown command '" + words[0] + "'");
        await Console.Out.WriteLineAsync(COMMANDS);
    }

    private static async Task<HeaderSnapshot> readHeaderAsync(ModbusGateway gateway, CancellationToken ct)
    {
        ushort[] raw = await gateway.readInputRegistersAsync(0, RegisterMap.HEADER_REGISTER_COUNT, ct); // one request
        return HeaderSnapshot.decode(raw);
    }

    private static async Task printHeaderAsync(HeaderSnapshot header)
    {
        await Console.Out.WriteLineAsync("raw    " + string.Join(" ", header.Raw));
        await Console.Out.WriteLineAsync("type   " + header.Type);
        await Console.Out.WriteLineAsync("state  " + header.State);
        await Console.Out.WriteLineAsync("fault  " + header.FaultCode);
        await Console.Out.WriteLineAsync("warn   " + header.warnText());
        await Console.Out.WriteLineAsync("power  " + header.ActivePowerW + " W");
        await Console.Out.WriteLineAsync("energy " + header.EnergyWh + " Wh");
        await Console.Out.WriteLineAsync("time   " + header.VirtualTime.UtcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture));
        await Console.Out.WriteLineAsync("seq    " + header.Sequence);
        await Console.Out.WriteLineAsync("gen    " + header.Generation);
        await Console.Out.WriteLineAsync("clock  " + header.ClockState);
    }

    private static async Task deviceAsync(ModbusGateway gateway, CancellationToken ct)
    {
        HeaderSnapshot header = await readHeaderAsync(gateway, ct);
        IReadOnlyList<DeviceProfileValue> profile = DeviceProfiles.forType(header.Type);
        ushort[] raw = await gateway.readInputRegistersAsync(RegisterMap.SPECIFIC_START, profile.Count * RegisterMap.SPECIFIC_REGISTERS_PER_VALUE, ct);
        await Console.Out.WriteLineAsync("device " + header.Type + ", " + profile.Count + " values");
        for (int index = 0; index < profile.Count; index++)
        {
            DeviceProfileValue value = profile[index];
            int rawValue = RegisterCodec.readInt32(raw, index * RegisterMap.SPECIFIC_REGISTERS_PER_VALUE);
            double scaled = RegisterCodec.rawToScaled(rawValue, value.Scale);
            await Console.Out.WriteLineAsync(value.Name.PadRight(18) + "raw " + rawValue.ToString(CultureInfo.InvariantCulture).PadLeft(10) + "  = " + scaled.ToString("0.###", CultureInfo.InvariantCulture) + " " + value.Unit);
        }
    }

    private static async Task writeAsync(ModbusGateway gateway, string[] words, CancellationToken ct)
    {
        int address;
        int value;
        string text = string.Empty;
        if (words.Length >= 2 && words[1].ToLowerInvariant() == "on")
        {
            address = RegisterMap.HOLD_ON_OFF;
            value = 1;
            text = "on";
        }
        else if (words.Length >= 2 && words[1].ToLowerInvariant() == "off")
        {
            address = RegisterMap.HOLD_ON_OFF;
            value = 0;
            text = "off";
        }
        else if (words.Length >= 2 && words[1].ToLowerInvariant() == "reset")
        {
            address = RegisterMap.HOLD_FAULT_RESET;
            value = 1;
            text = "fault reset";
        }
        else if (words.Length >= 3 && words[1].ToLowerInvariant() == "limit")
        {
            double percent;
            if (!double.TryParse(words[2], NumberStyles.Float, CultureInfo.InvariantCulture, out percent) || percent < LIMIT_PERCENT_MIN || percent > LIMIT_PERCENT_MAX)
            {
                await Console.Out.WriteLineAsync("rejected, not sent: limit '" + words[2] + "' is outside " + LIMIT_PERCENT_MIN + ".." + LIMIT_PERCENT_MAX + " %");
                return;
            }

            address = RegisterMap.HOLD_LIMIT_PERMILLE;
            value = (int)Math.Round(percent * PERMILLE_PER_PERCENT);
            text = "limit " + percent.ToString("0.#", CultureInfo.InvariantCulture) + " %";
        }
        else
        {
            await Console.Out.WriteLineAsync("usage: write on | write off | write limit <0..100> | write reset");
            return;
        }

        await gateway.writeSingleRegisterAsync(address, value, ct);
        await Console.Out.WriteLineAsync("written: " + text + " (holding " + address + " = " + value + ")");
        await Task.Delay(WRITE_EFFECT_DELAY_MS, ct);
        HeaderSnapshot header = await readHeaderAsync(gateway, ct);
        await Console.Out.WriteLineAsync("effect: state " + header.State + ", fault " + header.FaultCode + ", power " + header.ActivePowerW + " W, warn " + header.warnText() + ", seq " + header.Sequence);
        if (header.hasInvalidWrite())
        {
            await Console.Out.WriteLineAsync("WARNING: device flagged InvalidWrite (warn bit 2), value was not accepted");
        }
    }

    private static async Task watchAsync(ModbusGateway gateway, CancellationToken ct)
    {
        await Console.Out.WriteLineAsync("watch: header every 1 s, press a key to stop (redirected input: Ctrl+C)");
        while (true)
        {
            HeaderSnapshot header = await readHeaderAsync(gateway, ct);
            await Console.Out.WriteLineAsync(header.VirtualTime.UtcDateTime.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "Z  " + header.State + "  " + header.ActivePowerW + " W  " + header.EnergyWh + " Wh  seq " + header.Sequence + "  warn " + header.warnText() + "  clock " + header.ClockState);
            await Task.Delay(WATCH_INTERVAL_MS, ct);
            if (!Console.IsInputRedirected && Console.KeyAvailable)
            {
                Console.ReadKey(true); // swallow the stop key
                return;
            }
        }
    }

    /// <summary>Console.In blocks its thread, so on a terminal wait for the first key with a cancellable poll (keeps Ctrl+C responsive).</summary>
    private static async Task<string?> readLineAsync(CancellationToken ct)
    {
        if (!Console.IsInputRedirected)
        {
            while (!Console.KeyAvailable)
            {
                await Task.Delay(KEY_POLL_MS, ct);
            }
        }

        return await Console.In.ReadLineAsync(ct);
    }

    private static async Task<ModbusGateway> switchTargetAsync(DiagnosticSettings settings, ModbusGateway current, string[] words, CancellationToken ct)
    {
        string choice = string.Empty;
        if (words.Length >= 2)
        {
            choice = words[1];
        }
        else
        {
            for (int index = 0; index < settings.Targets.Count; index++)
            {
                await Console.Out.WriteLineAsync((index + 1) + ") " + describe(settings.Targets[index]));
            }

            await Console.Out.WriteAsync("select name or number: ");
            string? line = await readLineAsync(ct);
            if (line != null)
            {
                choice = line.Trim();
            }
        }

        DiagnosticTarget? selected = null;
        int number;
        foreach (DiagnosticTarget candidate in settings.Targets)
        {
            if (candidate.Name == choice)
            {
                selected = candidate;
            }
        }

        if (selected == null && int.TryParse(choice, out number) && number >= 1 && number <= settings.Targets.Count)
        {
            selected = settings.Targets[number - 1];
        }

        if (selected == null)
        {
            await Console.Out.WriteLineAsync("no such target '" + choice + "', keeping " + current.Target.Name);
            return current;
        }

        current.Dispose();
        await Console.Out.WriteLineAsync("target " + describe(selected));
        return new ModbusGateway(selected, settings.UnitId);
    }

    private static string describe(DiagnosticTarget target)
    {
        return target.Name + " " + target.Host + ":" + target.Port;
    }
}
