namespace Chamados.Domain;

/// <summary>
/// Critérios imutáveis para consulta de chamados.
/// </summary>
public sealed record FiltroChamados(
    StatusChamado? Status = null,
    Prioridade? PrioridadeMinima = null,
    string? Texto = null,
    OrdemChamados Ordem = OrdemChamados.Prioridade,
    int Pagina = 1,
    int TamanhoPagina = 10)
{
    public static FiltroChamados Todos { get; } = new();

    // Compatibilidade com chamadas posicionais usadas antes da paginação ficar separada.
    public FiltroChamados(
        StatusChamado? Status,
        Prioridade? PrioridadeMinima,
        string? Texto,
        int Pagina,
        int TamanhoPagina,
        OrdemChamados Ordem)
        : this(Status, PrioridadeMinima, Texto, Ordem, Pagina, TamanhoPagina)
    {
    }
}
