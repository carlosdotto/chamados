namespace Chamados.Domain;

public sealed class ChamadoEncerradoException(Guid chamadoId, StatusChamado status)
    : InvalidOperationException($"O chamado {chamadoId} está {Rotulo(status).ToLowerInvariant()} e não pode ser editado.")
{
    public Guid ChamadoId { get; } = chamadoId;

    public StatusChamado Status { get; } = status;

    private static string Rotulo(StatusChamado status) => status switch
    {
        StatusChamado.Aberto => "aberto",
        StatusChamado.EmAtendimento => "em atendimento",
        StatusChamado.Resolvido => "resolvido",
        StatusChamado.Cancelado => "cancelado",
        _ => status.ToString()
    };
}
