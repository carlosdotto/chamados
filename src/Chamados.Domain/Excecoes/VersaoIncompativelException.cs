namespace Chamados.Domain;

public sealed class VersaoIncompativelException(Guid chamadoId, int versaoAtual, int versaoEsperada)
    : InvalidOperationException($"O chamado {chamadoId} foi alterado por outra operação. Versão atual: {versaoAtual}. Versão esperada: {versaoEsperada}.")
{
    public Guid ChamadoId { get; } = chamadoId;

    public int VersaoAtual { get; } = versaoAtual;

    public int VersaoEsperada { get; } = versaoEsperada;
}
