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

## Decisões

- A solução usa `.slnx`, formato atual criado pelo SDK .NET 10.
- .NET 10 LTS é o alvo, com suporte previsto até 14/11/2028.
- `IChamadoRepositorio` separa o contrato do armazenamento em memória para permitir a troca por Entity Framework Core no Módulo 7.
- `TimeProvider` é injetado para tornar datas e durações determinísticas nos testes.
- Os casos de uso permanecem em `Chamados.Domain`; não há um projeto Application enquanto a separação não trouxer benefício concreto.
- O repositório é Singleton porque mantém a coleção compartilhada entre requisições; o serviço é Scoped para limitar sua instância ao escopo de cada requisição.
