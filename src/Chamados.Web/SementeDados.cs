using Chamados.Domain;

namespace Chamados.Web;

/// <summary>Popula a base em memória com exemplos apenas em Development.</summary>
public static class SementeDados
{
    public static async Task PopularAsync(
        IServiceProvider services,
        CancellationToken cancellationToken = default)
    {
        await using var escopo = services.CreateAsyncScope();
        var servico = escopo.ServiceProvider.GetRequiredService<ChamadoServico>();

        await servico.AbrirAsync(
            new NovoChamado("Trocar lâmpada do corredor", Prioridade.Baixa),
            cancellationToken);

        var arCondicionado = await servico.AbrirAsync(
            new NovoChamado(
                "Ar-condicionado da sala 204 desligando",
                Prioridade.Alta,
                "Desliga após 10 minutos de uso"),
            cancellationToken);
        await servico.IniciarAtendimentoAsync(
            arCondicionado.Id,
            "Equipe de climatização",
            cancellationToken);

        var vazamento = await servico.AbrirAsync(
            new NovoChamado("Vazamento na copa", Prioridade.Critica, "Água sob a pia"),
            cancellationToken);
        await servico.IniciarAtendimentoAsync(vazamento.Id, "Hidráulica", cancellationToken);
        await servico.ResolverAsync(vazamento.Id, "Sifão substituído", cancellationToken);

        var projetor = await servico.AbrirAsync(
            new NovoChamado("Projetor sem imagem no auditório", Prioridade.Media),
            cancellationToken);
        await servico.CancelarAsync(projetor.Id, "Aberto em duplicidade", cancellationToken);

        await servico.AbrirAsync(
            new NovoChamado("Tomada solta na recepção", Prioridade.Alta),
            cancellationToken);

        var exemplos = new[]
        {
            new NovoChamado("Computador não liga na sala 12", Prioridade.Alta, "Equipamento da estação 4"),
            new NovoChamado("Pintura descascando na escada", Prioridade.Baixa),
            new NovoChamado("Rede wi-fi instável no laboratório", Prioridade.Critica),
            new NovoChamado("Cadeira quebrada na sala 108", Prioridade.Media),
            new NovoChamado("Impressora sem papel no financeiro", Prioridade.Baixa),
            new NovoChamado("Luz piscando no corredor B", Prioridade.Media),
            new NovoChamado("Porta com fechadura emperrada", Prioridade.Alta)
        };

        foreach (var exemplo in exemplos)
        {
            await servico.AbrirAsync(exemplo, cancellationToken);
        }
    }
}
