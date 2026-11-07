namespace EnergySim.Diagnostic;

/// <summary>One device Modbus server the client can talk to.</summary>
public sealed class DiagnosticTarget
{
    public DiagnosticTarget(string name, string host, int port)
    {
        Name = name;
        Host = host;
        Port = port;
    }

    public string Name { get; }

    public string Host { get; }

    public int Port { get; }
}
