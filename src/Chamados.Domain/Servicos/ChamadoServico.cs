namespace Chamados.Domain;

/// <summary>
/// Casos de uso dos chamados. Depende de abstrações recebidas por injeção:
/// o repositório (onde guardar) e o relógio (que horas são).
/// </summary>
public sealed class ChamadoServico(IChamadoRepositorio repositorio, TimeProvider relogio)
{
    public async Task<Chamado> AbrirAsync(NovoChamado comando, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var chamado = new Chamado(comando.Titulo, comando.Prioridade, comando.Descricao, relogio.GetUtcNow());
        await repositorio.AdicionarAsync(chamado, cancellationToken);
        return chamado;
    }

    public async Task<Chamado> EditarAsync(
        Guid id,
        string titulo,
        Prioridade prioridade,
        string? descricao,
        int versaoEsperada,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(titulo);

        return await AlterarAsync(id, chamado =>
        {
            if (chamado.Versao != versaoEsperada)
            {
                throw new ConcorrenciaException(id, versaoEsperada, chamado.Versao);
            }

            chamado.Editar(titulo, prioridade, descricao);
        }, cancellationToken);
    }

    public Task<Chamado> EditarAsync(
        Guid id,
        EdicaoChamado edicao,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(edicao);
        return EditarAsync(id, edicao.Titulo, edicao.Prioridade, edicao.Descricao,
            edicao.VersaoEsperada, cancellationToken);
    }

    /// <summary>Consulta um chamado. Devolve null quando o Id não existe.</summary>
    public Task<Chamado?> ObterAsync(Guid id, CancellationToken cancellationToken = default)
        => repositorio.ObterAsync(id, cancellationToken);

    public Task<Chamado> IniciarAtendimentoAsync(Guid id, string responsavel, CancellationToken cancellationToken = default)
        => AlterarAsync(id, chamado => chamado.IniciarAtendimento(responsavel, relogio.GetUtcNow()), cancellationToken);

    public Task<Chamado> ResolverAsync(Guid id, string solucao, CancellationToken cancellationToken = default)
        => AlterarAsync(id, chamado => chamado.Resolver(solucao, relogio.GetUtcNow()), cancellationToken);

    public Task<Chamado> CancelarAsync(Guid id, string motivo, CancellationToken cancellationToken = default)
        => AlterarAsync(id, chamado => chamado.Cancelar(motivo, relogio.GetUtcNow()), cancellationToken);

    public async Task<IReadOnlyList<ResumoChamado>> ListarAsync(
        FiltroChamados filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        var chamados = await ConsultarAsync(filtro, cancellationToken);
        return chamados.Select(ResumoChamado.De).ToList();
    }

    public async Task<Pagina<ResumoChamado>> ListarPaginaAsync(
        FiltroChamados filtro,
        CancellationToken cancellationToken = default)
        => await ListarPaginaAsync(filtro, Math.Max(1, filtro.Pagina),
            filtro.TamanhoPagina, cancellationToken);

    public async Task<Pagina<ResumoChamado>> ListarPaginaAsync(
        FiltroChamados filtro,
        int numero,
        int tamanhoPagina,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);
        ArgumentOutOfRangeException.ThrowIfLessThan(tamanhoPagina, 1);

        var filtrados = await ConsultarAsync(filtro, cancellationToken);
        var total = filtrados.Count;
        var totalPaginas = total == 0 ? 1 : (total + tamanhoPagina - 1) / tamanhoPagina;
        numero = Math.Clamp(numero, 1, totalPaginas);

        var itens = filtrados
            .Skip((numero - 1) * tamanhoPagina)
            .Take(tamanhoPagina)
            .Select(ResumoChamado.De)
            .ToList();

        return new Pagina<ResumoChamado>(itens, numero, tamanhoPagina, total);
    }

    private async Task<IReadOnlyList<Chamado>> ConsultarAsync(
        FiltroChamados filtro,
        CancellationToken cancellationToken)
    {
        var chamados = await repositorio.ListarAsync(cancellationToken);

        var filtrados = chamados
            .Where(c => filtro.Status is null || c.Status == filtro.Status)
            .Where(c => filtro.PrioridadeMinima is null || c.Prioridade >= filtro.PrioridadeMinima)
            .Where(c => string.IsNullOrWhiteSpace(filtro.Texto)
                        || c.Titulo.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase)
                        || (c.Descricao?.Contains(filtro.Texto, StringComparison.OrdinalIgnoreCase) ?? false));

        var cultura = System.Globalization.CultureInfo.GetCultureInfo("pt-BR");
        var comparadorTitulo = StringComparer.Create(
            cultura,
            System.Globalization.CompareOptions.IgnoreCase |
            System.Globalization.CompareOptions.IgnoreNonSpace);

        IReadOnlyList<Chamado> ordenados = filtro.Ordem switch
        {
            OrdemChamados.MaisRecentes => filtrados.OrderByDescending(c => c.CriadoEm).ToList(),
            OrdemChamados.MaisAntigos => filtrados.OrderBy(c => c.CriadoEm).ToList(),
            OrdemChamados.Titulo => filtrados.OrderBy(c => c.Titulo, comparadorTitulo).ToList(),
            _ => filtrados
                .OrderByDescending(c => c.Prioridade)
                .ThenBy(c => c.CriadoEm)
                .ToList()
        };

        return ordenados;
    }

    public async Task<EstatisticasChamados> ObterEstatisticasAsync(CancellationToken cancellationToken = default)
    {
        var chamados = await repositorio.ListarAsync(cancellationToken);

        // Todos os status aparecem, mesmo com zero: parte do enum, não dos dados.
        var porStatus = Enum.GetValues<StatusChamado>()
            .ToDictionary(status => status, status => chamados.Count(c => c.Status == status));

        // Só as prioridades presentes aparecem: GroupBy agrupa o que existe.
        var porPrioridade = chamados
            .GroupBy(c => c.Prioridade)
            .ToDictionary(grupo => grupo.Key, grupo => grupo.Count());

        var temposDeResolucao = chamados
            .Where(c => c.Status == StatusChamado.Resolvido)
            .Select(c => c.TempoAteEncerramento!.Value)
            .ToList();

        TimeSpan? tempoMedio = temposDeResolucao.Count == 0
            ? null
            : TimeSpan.FromTicks((long)temposDeResolucao.Average(t => t.Ticks));

        return new EstatisticasChamados(chamados.Count, porStatus, porPrioridade, tempoMedio);
    }

    private Task<Chamado> AlterarAsync(Guid id, Action<Chamado> alteracao, CancellationToken cancellationToken)
        => repositorio.AlterarAsync(id, alteracao, cancellationToken);
}