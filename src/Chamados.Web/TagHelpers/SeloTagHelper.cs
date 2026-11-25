using Chamados.Web.Apresentacao;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Chamados.Web.TagHelpers;

[HtmlTargetElement("selo", Attributes = "valor", TagStructure = TagStructure.WithoutEndTag)]
public sealed class SeloTagHelper : TagHelper
{
    public Enum? Valor { get; set; }

    public string? Prefixo { get; set; }

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (Valor is null)
        {
            output.SuppressOutput();
            return;
        }

        output.TagName = "span";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", Valor.ClasseSelo());
        output.Content.SetContent(Prefixo + Valor.Rotulo());
    }
}
