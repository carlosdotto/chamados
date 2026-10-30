namespace Chamados.Domain;

/// <summary>
/// Contrato de armazenamento de chamados. A implementação atual é em memória;
/// o Entity Framework Core substituirá essa persistência no Módulo 7.
/// </summary>
public interface IChamadoRepositorio
{
    Task<Chamado?> ObterAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Chamado>> ListarAsync(CancellationToken cancellationToken = default);

    Task AdicionarAsync(Chamado chamado, CancellationToken cancellationToken = default);

    Task AtualizarAsync(Chamado chamado, CancellationToken cancellationToken = default);
}
