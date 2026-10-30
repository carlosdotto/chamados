using Chamados.Domain;

namespace Chamados.Domain.Tests;

public sealed class ChamadoTests
{
    private static readonly DateTimeOffset Inicio =
        new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void NovoChamado_InicializaDadosEEstado()
    {
        var chamado = new Chamado("Lâmpada queimada", criadoEm: Inicio);

        Assert.NotEqual(Guid.Empty, chamado.Id);
        Assert.Equal("Lâmpada queimada", chamado.Titulo);
        Assert.Equal(Prioridade.Media, chamado.Prioridade);
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Equal(Inicio, chamado.CriadoEm);
        Assert.Null(chamado.EncerradoEm);
        Assert.False(chamado.EstaEncerrado);
    }

    [Fact]
    public void Construtor_AparaTituloEDescricao()
    {
        var chamado = new Chamado("  Lâmpada  ", descricao: "  Sala 2  ", criadoEm: Inicio);

        Assert.Equal("Lâmpada", chamado.Titulo);
        Assert.Equal("Sala 2", chamado.Descricao);
    }

    [Fact]
    public void Construtor_RejeitaTituloEmBranco()
    {
        Assert.Throws<ArgumentException>(() => new Chamado("  "));
    }

    [Fact]
    public void Construtor_AceitaPrioridadeEDescricaoOpcionais()
    {
        var chamado = new Chamado("Reparo", Prioridade.Critica, "Urgente", Inicio);

        Assert.Equal(Prioridade.Critica, chamado.Prioridade);
        Assert.Equal("Urgente", chamado.Descricao);
    }

    [Fact]
    public void IniciarAtendimento_AtribuiResponsavelERegistraHistorico()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);

        chamado.IniciarAtendimento("  Alex  ", Inicio.AddMinutes(5));

        Assert.Equal(StatusChamado.EmAtendimento, chamado.Status);
        Assert.Equal("Alex", chamado.Responsavel);
        Assert.Equal(
            new RegistroHistorico(
                StatusChamado.Aberto,
                StatusChamado.EmAtendimento,
                Inicio.AddMinutes(5),
                "Atribuído a Alex"),
            Assert.Single(chamado.Historico));
    }

    [Fact]
    public void Resolver_EncerraChamadoEmAtendimento()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);
        chamado.IniciarAtendimento("Alex", Inicio.AddMinutes(5));

        chamado.Resolver("  Peça trocada  ", Inicio.AddHours(2));

        Assert.Equal(StatusChamado.Resolvido, chamado.Status);
        Assert.Equal("Peça trocada", chamado.Solucao);
        Assert.Equal(Inicio.AddHours(2), chamado.EncerradoEm);
        Assert.True(chamado.EstaEncerrado);
        Assert.Equal(2, chamado.Historico.Count);
    }

    [Fact]
    public void Cancelar_PermiteCancelarChamadoAberto()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);

        chamado.Cancelar("Duplicado", Inicio.AddMinutes(2));

        Assert.Equal(StatusChamado.Cancelado, chamado.Status);
        Assert.Equal(Inicio.AddMinutes(2), chamado.EncerradoEm);
        Assert.Equal("Duplicado", Assert.Single(chamado.Historico).Observacao);
    }

    [Fact]
    public void Cancelar_PermiteCancelarChamadoEmAtendimento()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);
        chamado.IniciarAtendimento("Alex", Inicio.AddMinutes(1));

        chamado.Cancelar("Solicitação retirada", Inicio.AddMinutes(2));

        Assert.Equal(StatusChamado.Cancelado, chamado.Status);
        Assert.Equal(2, chamado.Historico.Count);
    }

    [Fact]
    public void Resolver_EmChamadoAbertoLancaExcecaoSemAlterarEstado()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);

        var exception = Assert.Throws<TransicaoInvalidaException>(
            () => chamado.Resolver("Concluído", Inicio.AddMinutes(1)));

        Assert.Equal(chamado.Id, exception.ChamadoId);
        Assert.Equal(StatusChamado.Aberto, exception.Atual);
        Assert.Equal(StatusChamado.Resolvido, exception.Pretendido);
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Empty(chamado.Historico);
    }

    [Fact]
    public void ChamadoResolvido_NaoPodeSerCancelado()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);
        chamado.IniciarAtendimento("Alex", Inicio.AddMinutes(1));
        chamado.Resolver("Concluído", Inicio.AddMinutes(2));

        Assert.Throws<TransicaoInvalidaException>(
            () => chamado.Cancelar("Motivo", Inicio.AddMinutes(3)));
        Assert.Equal(StatusChamado.Resolvido, chamado.Status);
        Assert.Equal(2, chamado.Historico.Count);
    }

    [Fact]
    public void ChamadoCancelado_NaoPodeIniciarAtendimento()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);
        chamado.Cancelar("Duplicado", Inicio.AddMinutes(1));

        Assert.Throws<TransicaoInvalidaException>(
            () => chamado.IniciarAtendimento("Alex", Inicio.AddMinutes(2)));
        Assert.Equal(StatusChamado.Cancelado, chamado.Status);
        Assert.Single(chamado.Historico);
    }

    [Fact]
    public void TransicaoComDataAnteriorAbertura_LancaExcecaoSemAlterarEstado()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);

        Assert.Throws<ArgumentOutOfRangeException>(
            () => chamado.IniciarAtendimento("Alex", Inicio.AddTicks(-1)));
        Assert.Equal(StatusChamado.Aberto, chamado.Status);
        Assert.Empty(chamado.Historico);
    }

    [Fact]
    public void HistoricoEReadOnlyETempoAteEncerramentoECalculado()
    {
        var chamado = new Chamado("Reparo", criadoEm: Inicio);
        chamado.IniciarAtendimento("Alex", Inicio.AddMinutes(1));
        chamado.Resolver("Concluído", Inicio.AddHours(2));

        Assert.Equal(TimeSpan.FromHours(2), chamado.TempoAteEncerramento);
        Assert.IsNotType<List<RegistroHistorico>>(chamado.Historico);
    }
}
