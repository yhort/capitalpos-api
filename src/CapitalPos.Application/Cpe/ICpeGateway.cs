using System.Text.Json;

namespace CapitalPos.Application.Cpe;

public interface ICpeGateway
{
    Task<CpeGatewayResponse> ObtenerEstadoAsync(CancellationToken cancellationToken = default);

    Task<CpeGatewayResponse> EmitirAsync(
        JsonElement request,
        CancellationToken cancellationToken = default);
}
