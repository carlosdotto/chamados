using Chamados.Domain;

namespace Chamados.Domain.Tests;

public class ChamadoServicoTests
{
    private static readonly DateTimeOffset Inicio = new(2026, 10, 5, 9, 0, 0, TimeSpan.Zero);

    private readonly RelogioFixo _relogio = new(Inicio);
    private readonly ChamadoServico _servico;

    public ChamadoServicoTests()
    {
        // xUnit cria uma instância da classe por teste: cada teste tem seu próprio repositório.
        _servico = new ChamadoServico(new RepositorioChamadosEmMemoria(_relogio), _relogio);
    }

    [Fact]
    public async Task AbrirAsync_UsaHorarioDoRelogio()
    {
        var chamado = await _servico.AbrirAsync(new NovoChamado("Tomada solta"));

        Assert.Equal(Inicio, chamado.CriadoEm);
    }

    [Fact]
    public async Task ListarAsync_OrdenaPorPrioridadeDecrescenteEDepoisPorAbertura()
    {
        await _servico.AbrirAsync(new NovoChamado("A", Prioridade.Baixa));
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        await _servico.AbrirAsync(new NovoChamado("B", Prioridade.Alta));
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        await _servico.AbrirAsync(new NovoChamado("C", Prioridade.Alta));

        var resultado = await _servico.ListarAsync(FiltroChamados.Todos);

        Assert.Equal(new[] { "B", "C", "A" }, resultado.Select(r => r.Titulo));
    }

    [Fact]
    public async Task ListarAsync_FiltraPorTextoNaDescricaoSemDiferenciarMaiusculas()
    {
        await _servico.AbrirAsync(new NovoChamado("Ar-condicionado", Prioridade.Alta, "Desliga na SALA 204"));
        await _servico.AbrirAsync(new NovoChamado("Lâmpada", Prioridade.Baixa));

        var resultado = await _servico.ListarAsync(new FiltroChamados(Texto: "sala"));

        Assert.Equal("Ar-condicionado", Assert.Single(resultado).Titulo);
    }

    [Fact]
    public async Task ListarAsync_CombinaStatusEPrioridadeMinima()
    {
        var alta = await _servico.AbrirAsync(new NovoChamado("Alta em atendimento", Prioridade.Alta));
        await _servico.IniciarAtendimentoAsync(alta.Id, "Equipe");
        var baixa = await _servico.AbrirAsync(new NovoChamado("Baixa em atendimento", Prioridade.Baixa));
        await _servico.IniciarAtendimentoAsync(baixa.Id, "Equipe");
        await _servico.AbrirAsync(new NovoChamado("Alta aberta", Prioridade.Alta));

        var filtro = new FiltroChamados(Status: StatusChamado.EmAtendimento, PrioridadeMinima: Prioridade.Media);
        var resultado = await _servico.ListarAsync(filtro);

        Assert.Equal("Alta em atendimento", Assert.Single(resultado).Titulo);
    }

    [Fact]
    public async Task ObterEstatisticasAsync_CalculaTotaisETempoMedio()
    {
        var c1 = await _servico.AbrirAsync(new NovoChamado("C1", Prioridade.Alta));
        var c2 = await _servico.AbrirAsync(new NovoChamado("C2", Prioridade.Alta));
        await _servico.AbrirAsync(new NovoChamado("C3", Prioridade.Baixa));

        await _servico.IniciarAtendimentoAsync(c1.Id, "Equipe");
        await _servico.IniciarAtendimentoAsync(c2.Id, "Equipe");
        _relogio.Avancar(TimeSpan.FromHours(1));
        await _servico.ResolverAsync(c1.Id, "Feito");
        _relogio.Avancar(TimeSpan.FromHours(2));
        await _servico.ResolverAsync(c2.Id, "Feito");

        var estatisticas = await _servico.ObterEstatisticasAsync();

        Assert.Equal(3, estatisticas.Total);
        Assert.Equal(2, estatisticas.PorStatus[StatusChamado.Resolvido]);
        Assert.Equal(0, estatisticas.PorStatus[StatusChamado.Cancelado]);
        Assert.Equal(2, estatisticas.PorPrioridade[Prioridade.Alta]);
        Assert.False(estatisticas.PorPrioridade.ContainsKey(Prioridade.Critica));
        Assert.Equal(TimeSpan.FromHours(2), estatisticas.TempoMedioResolucao); // (1h + 3h) / 2
    }

    [Fact]
    public async Task ObterEstatisticasAsync_SemResolvidos_TempoMedioNulo()
    {
        await _servico.AbrirAsync(new NovoChamado("C1"));

        var estatisticas = await _servico.ObterEstatisticasAsync();

        Assert.Null(estatisticas.TempoMedioResolucao);
    }

    [Fact]
    public async Task IniciarAtendimentoAsync_IdInexistente_LancaChamadoNaoEncontrado()
    {
        var id = Guid.NewGuid();

        var erro = await Assert.ThrowsAsync<ChamadoNaoEncontradoException>(
            () => _servico.IniciarAtendimentoAsync(id, "Equipe"));

        Assert.Equal(id, erro.ChamadoId);
    }

    [Fact]
    public async Task ListarAsync_TokenCancelado_LancaOperationCanceled()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _servico.ListarAsync(FiltroChamados.Todos, cts.Token));
    }

    [Fact]
    public void FiltroChamados_ComWith_CriaCopiaSemAlterarOriginal()
    {
        var original = new FiltroChamados(Status: StatusChamado.Aberto);

        var copia = original with { Texto = "sala" };

        Assert.Null(original.Texto);
        Assert.Equal(StatusChamado.Aberto, copia.Status);
        Assert.NotEqual(original, copia);
        Assert.Equal(new FiltroChamados(Status: StatusChamado.Aberto), original);
    }

    [Fact]
    public async Task ObterAsync_IdExistente_DevolveOChamado()
    {
        var aberto = await _servico.AbrirAsync(new NovoChamado("Tomada solta"));

        var encontrado = await _servico.ObterAsync(aberto.Id);

        Assert.Same(aberto, encontrado);
    }

    [Fact]
    public async Task ObterAsync_IdInexistente_DevolveNull()
    {
        Assert.Null(await _servico.ObterAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task EditarAsync_AtualizaCamposEIncrementaVersao()
    {
        var chamado = await _servico.AbrirAsync(new NovoChamado("Título original"));

        var atualizado = await _servico.EditarAsync(chamado.Id,
            new EdicaoChamado("Título novo", Prioridade.Alta, "Descrição nova", chamado.Versao));

        Assert.Equal("Título novo", atualizado.Titulo);
        Assert.Equal(Prioridade.Alta, atualizado.Prioridade);
        Assert.Equal("Descrição nova", atualizado.Descricao);
        Assert.Equal(2, atualizado.Versao);
    }

    [Fact]
    public async Task EditarAsync_VersaoDesatualizada_LancaConflitoComVersoes()
    {
        var chamado = await _servico.AbrirAsync(new NovoChamado("Título original"));
        await _servico.IniciarAtendimentoAsync(chamado.Id, "Equipe");

        var erro = await Assert.ThrowsAsync<ConcorrenciaException>(() =>
            _servico.EditarAsync(chamado.Id,
                new EdicaoChamado("Tentativa concorrente", Prioridade.Media, null, 1)));

        Assert.Equal(2, erro.VersaoAtual);
        Assert.Equal(1, erro.VersaoEsperada);
    }

    [Fact]
    public async Task ListarPaginaAsync_DevolveTotalFiltradoIndependentementeDaPagina()
    {
        for (var i = 0; i < 7; i++)
        {
            await _servico.AbrirAsync(new NovoChamado($"Chamado {i}", Prioridade.Media, "Sala A"));
        }

        await _servico.AbrirAsync(new NovoChamado("Outro", Prioridade.Media, "Sala B"));

        var resultado = await _servico.ListarPaginaAsync(
            new FiltroChamados(Texto: "sala a", Pagina: 2, TamanhoPagina: 3));

        Assert.Equal(7, resultado.TotalItens);
        Assert.Equal(3, resultado.TotalPaginas);
        Assert.Equal(2, resultado.Numero);
        Assert.Equal(3, resultado.Itens.Count);
    }

    [Fact]
    public async Task ListarPaginaAsync_OrdenaTituloComCulturaBrasileira()
    {
        await _servico.AbrirAsync(new NovoChamado("Zebra"));
        await _servico.AbrirAsync(new NovoChamado("Bala"));
        await _servico.AbrirAsync(new NovoChamado("Árvore"));

        var resultado = await _servico.ListarPaginaAsync(
            new FiltroChamados(TamanhoPagina: 5, Ordem: OrdemChamados.Titulo));

        Assert.Equal(new[] { "Árvore", "Bala", "Zebra" }, resultado.Itens.Select(item => item.Titulo));
    }

    [Fact]
    public async Task ListarPaginaAsync_UsaNumeroDaPaginaRecalculadoQuandoExcedeTotal()
    {
        for (var i = 0; i < 6; i++)
        {
            await _servico.AbrirAsync(new NovoChamado($"Chamado {i}", Prioridade.Media));
        }

        var resultado = await _servico.ListarPaginaAsync(FiltroChamados.Todos, 99, 5);

        Assert.Equal(2, resultado.Numero);
        Assert.Single(resultado.Itens);
        Assert.Equal(2, resultado.TotalPaginas);
    }

    [Fact]
    public async Task ListarPaginaAsync_Pagina0VoltaParaPrimeira()
    {
        await _servico.AbrirAsync(new NovoChamado("Chamado"));

        var resultado = await _servico.ListarPaginaAsync(FiltroChamados.Todos, 0, 5);

        Assert.Equal(1, resultado.Numero);
        Assert.Single(resultado.Itens);
    }

    [Fact]
    public async Task ListarPaginaAsync_TamanhoMenorQue1_LancaErro()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => _servico.ListarPaginaAsync(FiltroChamados.Todos, 1, 0));
    }

    [Fact]
    public async Task ListarAsync_NaoPerdeItensPorPaginacao()
    {
        for (var i = 0; i < 12; i++)
        {
            await _servico.AbrirAsync(new NovoChamado($"Chamado {i}", Prioridade.Media));
        }

        var resultado = await _servico.ListarAsync(FiltroChamados.Todos);

        Assert.Equal(12, resultado.Count);
    }

    [Fact]
    public async Task ListarPaginaAsync_OrdenaMaisRecentesPrimeiro()
    {
        var primeiro = await _servico.AbrirAsync(new NovoChamado("Primeiro"));
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        await _servico.AbrirAsync(new NovoChamado("Segundo"));

        var resultado = await _servico.ListarPaginaAsync(
            new FiltroChamados(Ordem: OrdemChamados.MaisRecentes), 1, 5);

        Assert.Equal("Segundo", resultado.Itens[0].Titulo);
        Assert.Equal(primeiro.Titulo, resultado.Itens[1].Titulo);
    }

    [Fact]
    public async Task ListarPaginaAsync_OrdenaMaisAntigosPrimeiro()
    {
        await _servico.AbrirAsync(new NovoChamado("Primeiro"));
        _relogio.Avancar(TimeSpan.FromMinutes(1));
        await _servico.AbrirAsync(new NovoChamado("Segundo"));

        var resultado = await _servico.ListarPaginaAsync(
            new FiltroChamados(Ordem: OrdemChamados.MaisAntigos), 1, 5);

        Assert.Equal("Primeiro", resultado.Itens[0].Titulo);
        Assert.Equal("Segundo", resultado.Itens[1].Titulo);
    }

    [Fact]
    public async Task Pagina_SemItens_ContinuaComUmaPaginaVazia()
    {
        var resultado = await _servico.ListarPaginaAsync(FiltroChamados.Todos, 1, 5);

        Assert.Equal(1, resultado.TotalPaginas);
        Assert.False(resultado.TemAnterior);
        Assert.False(resultado.TemProxima);
    }

    [Fact]
    public async Task FiltroChamados_AceitaOrdemNoQuartoParametro()
    {
        var filtro = new FiltroChamados(null, null, null, OrdemChamados.Titulo);

        Assert.Equal(OrdemChamados.Titulo, filtro.Ordem);
    }
}