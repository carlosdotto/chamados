namespace Chamados.Domain;

/// <summary>Dados de uma edição e a versão que o usuário leu antes de alterá-los.</summary>
public sealed record EdicaoChamado(
    string Titulo,
    Prioridade Prioridade,
    string? Descricao,
    int VersaoEsperada)
{
    public int Versao => VersaoEsperada;
}
