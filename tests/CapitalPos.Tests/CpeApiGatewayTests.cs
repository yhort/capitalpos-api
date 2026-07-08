using System.Net;
using System.Text;
using System.Text.Json;
using CapitalPos.Infrastructure.Cpe;

namespace CapitalPos.Tests;

public class CpeApiGatewayTests
{
    [Fact]
    public async Task Emitir_envia_post_a_endpoint_emitir_con_payload_json()
    {
        using var requestJson = JsonDocument.Parse("""
            {
                "rucEmisor": "20123456789",
                "tipoComprobante": "01",
                "serie": "F001",
                "correlativo": 1
            }
            """);
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(
                """{"ok":true,"mensaje":"Comprobante emitido correctamente."}""",
                Encoding.UTF8,
                "application/json")
        });
        var gateway = CrearGateway(handler);

        await gateway.EmitirAsync(requestJson.RootElement);

        Assert.NotNull(handler.Request);
        Assert.Equal(HttpMethod.Post, handler.Request.Method);
        Assert.Equal(
            new Uri("https://cpe.capitalpos.test/api/cpe/emitir"),
            handler.Request.RequestUri);
        Assert.True(handler.Request.Headers.TryGetValues(
            CpeApiOptions.ApiKeyHeaderName,
            out var apiKeyValues));
        Assert.Equal(["capitalpos-cpe-test-api-key"], apiKeyValues);
        Assert.Equal("application/json", handler.Request.Content?.Headers.ContentType?.MediaType);
        Assert.Contains(
            "\"rucEmisor\": \"20123456789\"",
            handler.RequestContent,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Emitir_devuelve_estado_content_type_y_cuerpo_de_cpe_api()
    {
        using var requestJson = JsonDocument.Parse("""{"rucEmisor":"20123456789"}""");
        var handler = new RecordingHandler(new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent(
                """{"ok":false,"mensaje":"El comprobante tiene errores."}""",
                Encoding.UTF8,
                "application/json")
        });
        var gateway = CrearGateway(handler);

        var response = await gateway.EmitirAsync(requestJson.RootElement);

        Assert.Equal(400, response.StatusCode);
        Assert.False(response.IsSuccessStatusCode);
        Assert.Equal("""{"ok":false,"mensaje":"El comprobante tiene errores."}""", response.Content);
        Assert.StartsWith("application/json", response.ContentType, StringComparison.Ordinal);
    }

    private static CpeApiGateway CrearGateway(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("https://cpe.capitalpos.test/")
        };
        httpClient.DefaultRequestHeaders.Add(
            CpeApiOptions.ApiKeyHeaderName,
            "capitalpos-cpe-test-api-key");

        return new CpeApiGateway(httpClient);
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public RecordingHandler(HttpResponseMessage response)
        {
            _response = response;
        }

        public HttpRequestMessage Request { get; private set; } = null!;

        public string RequestContent { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            RequestContent = request.Content is null
                ? string.Empty
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return _response;
        }
    }
}
