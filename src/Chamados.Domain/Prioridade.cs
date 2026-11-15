namespace Chamados.Domain;

/// <summary>
/// Urgência do chamado. Os valores explícitos definem a ordenação de prioridade.
/// </summary>
public enum Prioridade
{
    Baixa = 1,
    Media = 2,
    Alta = 3,
    Critica = 4
}
