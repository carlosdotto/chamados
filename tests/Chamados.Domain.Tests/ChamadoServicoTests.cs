using Chamados.Domain;

namespace Chamados.Domain.Tests;

public sealed class ChamadoServicoTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AbrirAsync_UsaRelogioInjetadoEAdicionaChamado()
    {
        var relogio = new RelogioFixo(Inicio);
        var repositorio = new RepositorioChamadosEmMemoria(relogio);
        var servico = new ChamadoServico(repositorio, relogio);
        relogio.Avancar(TimeSpan.FromMinutes(3));

        var chamado = await servico.AbrirAsync(new NovoChamado("Reparo"));
        var salvo = await repositorio.ObterAsync(chamado.Id);

        Assert.Equal(Inicio.AddMinutes(3), chamado.CriadoEm);
        Assert.Same(chamado, salvo);
    }

    [Fact]
    public async Task ListarAsync_OrdenaPorPrioridadeEDepoisPorAbertura()
    {
        var (servico, relogio) = CriarServico();
        await servico.AbrirAsync(new NovoChamado("Alta antiga", Prioridade.Alta));
        relogio.Avancar(TimeSpan.FromMinutes(1));
        await servico.AbrirAsync(new NovoChamado("Crítica", Prioridade.Critica));
        relogio.Avancar(TimeSpan.FromMinutes(1));
        await servico.AbrirAsync(new NovoChamado("Alta nova", Prioridade.Alta));

        var chamados = await servico.ListarAsync(FiltroChamados.Todos);

        Assert.Equal(["Crítica", "Alta antiga", "Alta nova"], chamados.Select(c => c.Titulo));
    }

    [Fact]
    public async Task ListarAsync_BuscaTextoNaDescricaoSemDiferenciarMaiusculas()
    {
        var (servico, _) = CriarServico();
        await servico.AbrirAsync(
            new NovoChamado("Ar condicionado", Prioridade.Alta, "Desliga após dez minutos"));
        await servico.AbrirAsync(new NovoChamado("Lâmpada", Prioridade.Baixa, "Corredor"));

        var chamados = await servico.ListarAsync(new FiltroChamados(Texto: "DEZ"));

        Assert.Equal("Ar condicionado", Assert.Single(chamados).Titulo);
    }

    [Fact]
    public async Task ListarAsync_CompoeFiltroDeStatusEDePrioridadeMinima()
    {
        var (servico, relogio) = CriarServico();
        var emAtendimento = await servico.AbrirAsync(
            new NovoChamado("Prioridade alta", Prioridade.Alta));
        await servico.IniciarAtendimentoAsync(emAtendimento.Id, "Equipe");
        await servico.AbrirAsync(new NovoChamado("Aberto crítico", Prioridade.Critica));
        var prioridadeBaixa = await servico.AbrirAsync(
            new NovoChamado("Atendimento baixo", Prioridade.Baixa));
        relogio.Avancar(TimeSpan.FromMinutes(1));
        await servico.IniciarAtendimentoAsync(prioridadeBaixa.Id, "Equipe");

        var chamados = await servico.ListarAsync(
            new FiltroChamados(StatusChamado.EmAtendimento, Prioridade.Alta));

        Assert.Equal("Prioridade alta", Assert.Single(chamados).Titulo);
    }

    [Fact]
    public async Task ObterEstatisticasAsync_CalculaTempoMedioComRelogioControlado()
    {
        var (servico, relogio) = CriarServico();
        var primeiro = await servico.AbrirAsync(new NovoChamado("Primeiro"));
        relogio.Avancar(TimeSpan.FromHours(2));
        await servico.IniciarAtendimentoAsync(primeiro.Id, "Equipe");
        await servico.ResolverAsync(primeiro.Id, "Concluído");

        var segundo = await servico.AbrirAsync(new NovoChamado("Segundo"));
        relogio.Avancar(TimeSpan.FromHours(4));
        await servico.IniciarAtendimentoAsync(segundo.Id, "Equipe");
        await servico.ResolverAsync(segundo.Id, "Concluído");

        var estatisticas = await servico.ObterEstatisticasAsync();

        Assert.Equal(TimeSpan.FromHours(3), estatisticas.TempoMedioResolucao);
    }

    [Fact]
    public async Task AlterarAsync_IdInexistenteLancaChamadoNaoEncontrado()
    {
        var (servico, _) = CriarServico();

        await Assert.ThrowsAsync<ChamadoNaoEncontradoException>(
            () => servico.ResolverAsync(Guid.NewGuid(), "Concluído"));
    }

    [Fact]
    public async Task ListarAsync_RespeitaTokenJaCancelado()
    {
        var (servico, _) = CriarServico();
        using var fonte = new CancellationTokenSource();
        fonte.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => servico.ListarAsync(FiltroChamados.Todos, fonte.Token));
    }

    [Fact]
    public async Task IniciarAtendimentoAsync_PersisteTransicao()
    {
        var (servico, _) = CriarServico();
        var chamado = await servico.AbrirAsync(new NovoChamado("Reparo"));

        var atualizado = await servico.IniciarAtendimentoAsync(chamado.Id, "Equipe");
        var salvo = await servico.ListarAsync(new FiltroChamados(StatusChamado.EmAtendimento));

        Assert.Equal(StatusChamado.EmAtendimento, atualizado.Status);
        Assert.Equal(chamado.Id, Assert.Single(salvo).Id);
    }

    [Fact]
    public async Task ObterEstatisticasAsync_IncluiStatusSemChamados()
    {
        var (servico, _) = CriarServico();
        await servico.AbrirAsync(new NovoChamado("Reparo", Prioridade.Alta));

        var estatisticas = await servico.ObterEstatisticasAsync();

        Assert.Equal(1, estatisticas.Total);
        Assert.Equal(Enum.GetValues<StatusChamado>().Length, estatisticas.PorStatus.Count);
        Assert.Equal(1, estatisticas.PorStatus[StatusChamado.Aberto]);
        Assert.Equal(0, estatisticas.PorStatus[StatusChamado.Resolvido]);
        Assert.Equal(new Dictionary<Prioridade, int> { [Prioridade.Alta] = 1 },
            estatisticas.PorPrioridade);
        Assert.Null(estatisticas.TempoMedioResolucao);
    }

    private static (ChamadoServico Servico, RelogioFixo Relogio) CriarServico()
    {
        var relogio = new RelogioFixo(Inicio);
        var repositorio = new RepositorioChamadosEmMemoria(relogio);
        return (new ChamadoServico(repositorio, relogio), relogio);
    }
}
