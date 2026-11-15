using Chamados.Domain;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Chamados.Web.Apresentacao;

[HtmlTargetElement("status-badge", Attributes = "status")]
public sealed class StatusChamadoTagHelper : TagHelper
{
    public StatusChamado Status { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "span";
        output.Attributes.SetAttribute("class", $"selo selo-{Status.ToString().ToLowerInvariant()}");
        output.Content.SetContent(Status.Rotulo());
    }
}
