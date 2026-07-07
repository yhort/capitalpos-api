namespace CapitalPos.Application.Seguridad;

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiraEn);
