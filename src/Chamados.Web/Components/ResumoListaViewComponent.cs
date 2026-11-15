using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;

namespace Chamados.Web.Components;

public sealed class ResumoListaViewComponent(ChamadoServico servico) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(CancellationToken cancellationToken = default)
    {
        var estatisticas = await servico.ObterEstatisticasAsync(cancellationToken);
        return View(estatisticas);
    }
}
