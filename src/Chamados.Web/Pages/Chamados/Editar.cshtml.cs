using Chamados.Domain;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chamados.Web.Pages.Chamados;

public sealed class EditarModel(ChamadoServico servico) : PageModel
{
    [BindProperty]
    public ChamadoInput Input { get; set; } = new();

    public Guid Id { get; private set; }

    public Chamado? ChamadoAtual { get; private set; }

    public bool Conflito { get; private set; }

    public async Task<IActionResult> OnGetAsync(Guid id, CancellationToken cancellationToken)
    {
        var chamado = await servico.ObterAsync(id, cancellationToken);
        if (chamado is null)
        {
            return NotFound();
        }

        if (chamado.EstaEncerrado)
        {
            TempData["Mensagem"] = "Chamados resolvidos ou cancelados não podem ser editados.";
            return RedirectToPage("./Detalhes", new { id });
        }

        Id = chamado.Id;
        ChamadoAtual = chamado;
        Input = ChamadoInput.De(chamado);

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
            var chamado = await servico.EditarAsync(id,
                new EdicaoChamado(Input.Titulo, Input.Prioridade, Input.Descricao, Input.Versao),
                cancellationToken);
            TempData["Mensagem"] = $"Chamado \"{chamado.Titulo}\" atualizado.";
            return RedirectToPage("./Detalhes", new { id = chamado.Id });
        }
        catch (ChamadoNaoEncontradoException)
        {
            return NotFound();
        }
        catch (ConcorrenciaException)
        {
            var atual = await servico.ObterAsync(id, cancellationToken);
            if (atual is null)
            {
                return NotFound();
            }

            Id = atual.Id;
            ChamadoAtual = atual;
            Conflito = true;
            Input.Versao = atual.Versao;
            ModelState.Remove("Input.Versao");
            ModelState.AddModelError(string.Empty,
                "Este chamado foi alterado em outra aba. Confira os valores atuais e tente salvar novamente.");
            return Page();
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
