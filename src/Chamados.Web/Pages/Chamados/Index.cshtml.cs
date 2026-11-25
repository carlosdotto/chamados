using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chamados.Web.Pages.Chamados;

/// <summary>Lista com filtros. O formulário de filtro usa GET: a busca fica na URL e pode ser compartilhada.</summary>
public sealed class IndexModel(ChamadoServico servico) : PageModel
{
    private const int TamanhoPagina = 5;

    [BindProperty(SupportsGet = true)]
    public StatusChamado? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Prioridade? PrioridadeMinima { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Texto { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    [BindProperty(SupportsGet = true)]
    public OrdemChamados Ordem { get; set; } = OrdemChamados.Prioridade;

    public int PaginaAtual => Pagina < 1 ? 1 : Pagina;

    public int TotalPaginas { get; private set; } = 1;

    public IReadOnlyList<ResumoChamado> Chamados { get; private set; } = [];

    public int TotalItens { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Status = null;
            PrioridadeMinima = null;
        }

        Pagina = PaginaAtual;
        if (!Enum.IsDefined(Ordem))
        {
            Ordem = OrdemChamados.Prioridade;
        }

        var resultado = await servico.ListarPaginaAsync(
            new FiltroChamados(Status, PrioridadeMinima, Texto, Pagina, TamanhoPagina, Ordem),
            cancellationToken);
        TotalItens = resultado.TotalItens;
        TotalPaginas = resultado.TotalPaginas;

        if (Pagina > TotalPaginas)
        {
            Pagina = TotalPaginas;
            resultado = await servico.ListarPaginaAsync(
                new FiltroChamados(Status, PrioridadeMinima, Texto, Pagina, TamanhoPagina, Ordem),
                cancellationToken);
        }

        Chamados = resultado.Itens;
    }

    public Dictionary<string, string?> RotaPara(int pagina) => new()
    {
        ["status"] = Status?.ToString(),
        ["prioridadeMinima"] = PrioridadeMinima?.ToString(),
        ["texto"] = Texto,
        ["ordem"] = Ordem.ToString(),
        ["pagina"] = pagina.ToString(System.Globalization.CultureInfo.InvariantCulture)
    };
}