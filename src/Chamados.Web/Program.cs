using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Chamados.Domain;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.MapGet("/", () => "Chamados.Web em execução");

app.MapGet("/status", () => Enum.GetNames<StatusChamado>());
app.MapGet("/chamados/exemplo", () => new Chamado("Trocar lâmpada do corredor"));

// Diagnóstico exposto somente em Development:
// em produção, revelar versão de runtime ajuda um atacante.
if (app.Environment.IsDevelopment())
{
    app.MapGet("/ambiente", (IHostEnvironment env) => new
    {
        env.EnvironmentName,
        Runtime = RuntimeInformation.FrameworkDescription
    });
}
app.Run();