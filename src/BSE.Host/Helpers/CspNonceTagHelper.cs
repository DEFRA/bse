using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace BSE.Host.Helpers;

/// <summary>Adds the per-request CSP nonce to every inline-capable &lt;script&gt; element.</summary>
[HtmlTargetElement("script")]
public sealed class CspNonceTagHelper : TagHelper
{
    [ViewContext, HtmlAttributeNotBound]
    public ViewContext ViewContext { get; set; } = null!;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        if (output.Attributes.ContainsName("nonce"))
            return;

        if (ViewContext.HttpContext.Items[Middleware.SecurityHeadersMiddleware.NonceItemKey] is string nonce)
            output.Attributes.SetAttribute("nonce", nonce);
    }
}
