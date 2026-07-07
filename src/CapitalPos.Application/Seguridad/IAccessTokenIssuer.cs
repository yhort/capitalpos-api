namespace CapitalPos.Application.Seguridad;

public interface IAccessTokenIssuer
{
    AccessTokenResult Emitir(AccessTokenRequest request);
}
