namespace Chamados.Web.Components.Fila;

/// <summary>As ações que um cartão da fila pode solicitar.</summary>
public enum TipoTransicao
{
    Iniciar,
    Resolver,
    Cancelar
}

/// <summary>Pedido enviado pela fila para a página executar a transição.</summary>
public sealed record PedidoTransicao(Guid ChamadoId, TipoTransicao Tipo, string Texto);
