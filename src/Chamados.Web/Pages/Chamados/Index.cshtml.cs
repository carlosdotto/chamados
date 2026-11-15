using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chamados.Web.Pages.Chamados;

/// <summary>Lista com filtros. O formulário de filtro usa GET: a busca fica na URL e pode ser compartilhada.</summary>
public sealed class IndexModel(ChamadoServico servico) : PageModel
{
    private const int TamanhoPagina = 10;

    [BindProperty(SupportsGet = true)]
    public StatusChamado? Status { get; set; }

    [BindProperty(SupportsGet = true)]
    public Prioridade? PrioridadeMinima { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? Texto { get; set; }

    [BindProperty(SupportsGet = true)]
    public int Pagina { get; set; } = 1;

    public int PaginaAtual => Pagina < 1 ? 1 : Pagina;

    public int TotalPaginas { get; private set; } = 1;

    public IReadOnlyList<ResumoChamado> Chamados { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            Status = null;
            PrioridadeMinima = null;
        }

        Pagina = PaginaAtual;
        var filtro = new FiltroChamados(Status, PrioridadeMinima, Texto, Pagina, TamanhoPagina);
        Chamados = await servico.ListarAsync(filtro, cancellationToken);

        if (Chamados.Count >= TamanhoPagina)
        {
            TotalPaginas = Pagina + 1;
        }
        else
        {
            TotalPaginas = Math.Max(1, Pagina);
        }
    }
}