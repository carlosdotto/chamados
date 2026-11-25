namespace Chamados.Domain;

/// <summary>Uma página de resultados e o total encontrado antes da paginação.</summary>
public sealed record Pagina<T>(
    IReadOnlyList<T> Itens,
    int Numero,
    int TamanhoPagina,
    int TotalItens)
{
    public int Total => TotalItens;

    public int PaginaAtual => Numero;

    public bool TemAnterior => Numero > 1;

    public bool TemProxima => Numero < TotalPaginas;

    public int TotalPaginas => TotalItens == 0
        ? 1
        : (TotalItens + TamanhoPagina - 1) / TamanhoPagina;
}
