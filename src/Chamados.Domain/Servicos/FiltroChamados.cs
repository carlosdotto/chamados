namespace Chamados.Domain;

/// <summary>
/// Critérios imutáveis para consulta de chamados.
/// </summary>
public sealed record FiltroChamados(
    StatusChamado? Status = null,
    Prioridade? PrioridadeMinima = null,
    string? Texto = null,
    int Pagina = 1,
    int TamanhoPagina = 10)
{
    public static FiltroChamados Todos { get; } = new();
}
