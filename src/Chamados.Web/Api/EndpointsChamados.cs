using Chamados.Domain;

namespace Chamados.Web.Api;

/// <summary>
/// API JSON do Módulo 2, agora agrupada sob /api para não disputar rotas com as páginas.
/// </summary>
public static class EndpointsChamados
{
    public static RouteGroupBuilder MapApiChamados(this IEndpointRouteBuilder app)
    {
        var api = app.MapGroup("/api/chamados")
            .AddEndpointFilter<FiltroExcecoesDominio>();

        api.MapGet("/", (StatusChamado? status, Prioridade? prioridadeMinima, string? texto,
                int? pagina, int? tamanhoPagina, OrdemChamados? ordem,
                ChamadoServico servico, CancellationToken cancellationToken)
            => servico.ListarPaginaAsync(
                new FiltroChamados(status, prioridadeMinima, texto, pagina ?? 1, tamanhoPagina ?? 5,
                    ordem ?? OrdemChamados.Prioridade), cancellationToken));

        api.MapGet("/estatisticas", (ChamadoServico servico, CancellationToken cancellationToken)
            => servico.ObterEstatisticasAsync(cancellationToken));

        api.MapGet("/{id:guid}", async (Guid id, ChamadoServico servico, CancellationToken cancellationToken)
            => await servico.ObterAsync(id, cancellationToken) is { } chamado
                ? Results.Ok(ResumoChamado.De(chamado))
                : Results.NotFound());

        api.MapPost("/", async (NovoChamado comando, ChamadoServico servico, CancellationToken cancellationToken) =>
        {
            var chamado = await servico.AbrirAsync(comando, cancellationToken);
            return Results.Created($"/api/chamados/{chamado.Id}", ResumoChamado.De(chamado));
        });

        api.MapPut("/{id:guid}", async (Guid id, AtualizarChamadoRequest requisicao,
                ChamadoServico servico, CancellationToken cancellationToken) =>
        {
            var chamado = await servico.EditarAsync(id,
                new EdicaoChamado(requisicao.Titulo, requisicao.Prioridade, requisicao.Descricao,
                    requisicao.VersaoEsperada),
                cancellationToken);
            return Results.Ok(ResumoChamado.De(chamado));
        });

        // Sem try/catch: o FiltroExcecoesDominio responde 404, 409 ou 400.
        api.MapPost("/{id:guid}/atendimento", async (Guid id, IniciarAtendimentoRequest requisicao,
                ChamadoServico servico, CancellationToken cancellationToken)
            => ResumoChamado.De(await servico.IniciarAtendimentoAsync(id, requisicao.Responsavel, cancellationToken)));

        api.MapPost("/{id:guid}/resolucao", async (Guid id, ResolverRequest requisicao,
                ChamadoServico servico, CancellationToken cancellationToken)
            => ResumoChamado.De(await servico.ResolverAsync(id, requisicao.Solucao, cancellationToken)));

        api.MapPost("/{id:guid}/cancelamento", async (Guid id, CancelarRequest requisicao,
                ChamadoServico servico, CancellationToken cancellationToken)
            => ResumoChamado.De(await servico.CancelarAsync(id, requisicao.Motivo, cancellationToken)));

        return api;
    }
}

/// <summary>Dados para iniciar o atendimento de um chamado.</summary>
public sealed record IniciarAtendimentoRequest(string Responsavel);

/// <summary>Dados para registrar a solução de um chamado.</summary>
public sealed record ResolverRequest(string Solucao);

/// <summary>Dados para cancelar um chamado.</summary>
public sealed record CancelarRequest(string Motivo);

/// <summary>Dados para atualizar um chamado com controle otimista de concorrência.</summary>
public sealed record AtualizarChamadoRequest(
    string Titulo,
    Prioridade Prioridade,
    string? Descricao,
    int VersaoEsperada);