# Chamados

Sistema de chamados de manutenção predial, desenvolvido com .NET 10.

## Requisitos

- .NET 10 SDK; `global.json` pede 10.0.100 e permite avançar para uma feature band posterior com `latestFeature`.

## Executar e testar

```bash
dotnet build
dotnet run --project src/Chamados.Web
dotnet test
```

Em Development, a aplicação inicia com cinco chamados de exemplo. Com o perfil `http`, acesse o painel em `http://localhost:5080/`, a lista em `http://localhost:5080/Chamados` ou crie um chamado em `http://localhost:5080/Chamados/Novo`. A API JSON fica sob `/api/chamados`; use as requisições de [Chamados.Web.http](./src/Chamados.Web/Chamados.Web.http).

## Páginas Blazor

| Rota | Finalidade |
| --- | --- |
| `/modos` | Compara a renderização estática e o modo interativo no servidor. |
| `/atendimento` | Fila interativa para iniciar, resolver e cancelar chamados. |

## Decisões

- A solução usa `.slnx`, formato atual criado pelo SDK .NET 10.
- .NET 10 LTS é o alvo, com suporte previsto até 14/11/2028.
- `IChamadoRepositorio` separa o contrato do armazenamento em memória para permitir a troca por Entity Framework Core no Módulo 7.
- `TimeProvider` é injetado para tornar datas e durações determinísticas nos testes.
- Os casos de uso permanecem em `Chamados.Domain`; não há um projeto Application enquanto a separação não trouxer benefício concreto.
- O repositório é Singleton porque mantém a coleção compartilhada entre requisições; o serviço é Scoped para limitar sua instância ao escopo de cada requisição.
- Os componentes Razor usam o mesmo projeto Web e compartilham o domínio existente; não foi criado outro projeto.
- A interatividade é declarada por página. Apenas `/atendimento` usa o modo interativo do servidor; `/modos` demonstra os modos estático e interativo.
- `CartaoChamado` comunica pedidos com `EventCallback<PedidoTransicao>`; somente `Atendimento.razor` chama `ChamadoServico` e trata erros de transição.
