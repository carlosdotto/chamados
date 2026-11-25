namespace Chamados.Domain;

public enum OrdemChamados
{
    Prioridade,
    MaisRecentes,
    MaisAntigos,
    Titulo,
    CriadoMaisRecente = MaisRecentes,
    CriadoMaisAntigo = MaisAntigos
}
