namespace CapitalPos.Application.Seguridad;

public sealed record PasswordVerificationResult(bool EsValida, bool RequiereRehash);
