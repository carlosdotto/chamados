using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Chamados.Web.ViewComponents;

public sealed class ResumoStatusViewComponent(ChamadoServico servico) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var estatisticas = await servico.ObterEstatisticasAsync(cancellationToken);
        var resumo = new ResumoStatus(
            estatisticas.PorStatus[StatusChamado.Aberto],
            estatisticas.PorStatus[StatusChamado.EmAtendimento]);
        return View(resumo);
    }
}

public sealed record ResumoStatus(int Abertos, int EmAtendimento);
