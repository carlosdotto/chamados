using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Chamados.Domain;
using Chamados.Web;
using Chamados.Web.Api;
using Chamados.Web.Configuracao;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddOptions<SimulacaoOptions>()
    .BindConfiguration(SimulacaoOptions.Secao)
    .ValidateDataAnnotations()
    .ValidateOnStart();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IChamadoRepositorio>(services =>
    new RepositorioChamadosEmMemoria(
        services.GetRequiredService<TimeProvider>(),
        TimeSpan.FromMilliseconds(services.GetRequiredService<IOptions<SimulacaoOptions>>().Value.LatenciaMs)));
builder.Services.AddScoped<ChamadoServico>();
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseAntiforgery();
app.UseWhen(
    context => !context.Request.Path.StartsWithSegments("/api"),
    branch =>
    {
    branch.UseExceptionHandler("/Erro/500");
    branch.UseStatusCodePagesWithReExecute("/Erro/{0}");
    });

if (app.Environment.IsDevelopment())
{
    await SementeDados.PopularAsync(app.Services);
}

app.MapGet("/status", () => Enum.GetNames<StatusChamado>());
app.MapGet("/chamados/exemplo", () => new Chamado("Trocar lâmpada do corredor"));
app.MapApiChamados();
app.MapRazorPages();
app.MapRazorComponents<Chamados.Web.Components.App>()
    .AddInteractiveServerRenderMode();

if (app.Environment.IsDevelopment())
{
    app.MapGet("/ambiente", (IHostEnvironment environment) => new
    {
        environment.EnvironmentName,
        Runtime = RuntimeInformation.FrameworkDescription
    });
}

app.Run();
