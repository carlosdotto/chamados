using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chamados.Web.Pages.Chamados;

public sealed class EditarModel(ChamadoServico servico) : PageModel
{
    [BindProperty]
    public NovoChamadoInput Input { get; set; } = new();

    public Guid Id { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var chamado = await servico.ObterAsync(id, cancellationToken);
        if (chamado is null)
        {
            return NotFound();
        }

        Id = chamado.Id;
        Input = new NovoChamadoInput
        {
            Titulo = chamado.Titulo,
            Prioridade = chamado.Prioridade,
            Descricao = chamado.Descricao,
            Versao = chamado.Versao
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            var chamado = await servico.EditarAsync(id, Input.Titulo, Input.Prioridade, Input.Descricao, Input.Versao, cancellationToken);
            TempData["Mensagem"] = $"Chamado \"{chamado.Titulo}\" atualizado.";
            return RedirectToPage("./Detalhes", new { id = chamado.Id });
        }
        catch (ChamadoNaoEncontradoException)
        {
            return NotFound();
        }
        catch (VersaoIncompativelException)
        {
            ModelState.AddModelError(string.Empty, "Este chamado foi atualizado em outra aba. Recarregue a página e tente novamente.");
            return await OnGetAsync(id, cancellationToken);
        }
        catch (ChamadoEncerradoException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return await OnGetAsync(id, cancellationToken);
        }
        catch (ArgumentException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
    }
}
