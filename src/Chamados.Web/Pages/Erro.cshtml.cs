using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Chamados.Web.Pages;

/// <summary>
/// Destino do UseExceptionHandler("/Erro") fora de Development.
/// Mostra só um código de rastreio: o detalhe da exceção fica no log, nunca na tela.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public sealed class ErroModel : PageModel
{
    public string CodigoRequisicao { get; private set; } = string.Empty;

    public int? Codigo { get; private set; }

    public string Mensagem { get; private set; } = "Houve um problema ao processar sua solicitação.";

    public void OnGet(int? codigo)
    {
        Codigo = codigo;
        if (Codigo is 404)
        {
            Mensagem = "A página ou recurso solicitado não foi encontrado.";
        }
        else if (Codigo is 500)
        {
            Mensagem = "O servidor encontrou um erro ao processar a solicitação.";
        }

        CodigoRequisicao = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }

    public void OnPost(int? codigo) => OnGet(codigo);
}