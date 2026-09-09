using System.Diagnostics.Metrics;

namespace MfaCrud.Api.Telemetry;

/// <summary>
/// Métricas do domínio, ao lado das que a instrumentação já dá de graça (duração de request,
/// status, chamadas HTTP de saída, GC e thread pool).
/// </summary>
public sealed class MfaCrudTelemetry
{
    public const string ServiceName = "mfacrud-api";
    public const string MeterName = "MfaCrud.Api";

    private readonly Counter<long> _productsWritten;
    private readonly Counter<long> _twoFactorDevicesRemoved;
    private readonly Counter<long> _roleChanges;

    public MfaCrudTelemetry(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create(MeterName);

        _productsWritten = meter.CreateCounter<long>(
            "mfacrud.products.written",
            unit: "{product}",
            description: "Products created, updated or deleted through the API.");

        _twoFactorDevicesRemoved = meter.CreateCounter<long>(
            "mfacrud.two_factor.devices_removed",
            unit: "{device}",
            description: "Authenticators removed by their own owner.");

        _roleChanges = meter.CreateCounter<long>(
            "mfacrud.users.role_changes",
            unit: "{change}",
            description: "Role sets replaced by an admin, by resulting role.");
    }

    /// <param name="operation">created, updated or deleted.</param>
    public void ProductWritten(string operation) =>
        _productsWritten.Add(1, new KeyValuePair<string, object?>("operation", operation));

    public void TwoFactorDeviceRemoved() => _twoFactorDevicesRemoved.Add(1);

    // Só o nome do papel: baixa cardinalidade e nada que identifique a pessoa.
    public void RoleChanged(string role) =>
        _roleChanges.Add(1, new KeyValuePair<string, object?>("role", role));
}
