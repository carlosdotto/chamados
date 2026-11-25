namespace Chamados.Domain;

public sealed class VersaoIncompativelException(Guid chamadoId, int versaoAtual, int versaoEsperada)
    : Exception($"A versão do chamado {chamadoId} não corresponde: atual {versaoAtual}, esperada {versaoEsperada}.")
{
    public Guid ChamadoId { get; } = chamadoId;
    public int VersaoAtual { get; } = versaoAtual;
    public int VersaoEsperada { get; } = versaoEsperada;
}
