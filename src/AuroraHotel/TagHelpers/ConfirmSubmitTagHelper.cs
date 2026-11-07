using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace AuroraHotel.TagHelpers;

/// <summary>Hiện bước xác nhận bằng HTML gốc, không cần JavaScript.</summary>
[HtmlTargetElement("confirm-submit")]
public sealed class ConfirmSubmitTagHelper : TagHelper
{
    public string Message { get; set; } = "Bạn muốn tiếp tục thao tác này?";
    public string Label { get; set; } = "Tiếp tục";
    public string ButtonClass { get; set; } = "btn btn-primary";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "details";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "confirm-action");
        var summary = new TagBuilder("summary");
        summary.AddCssClass(ButtonClass);
        summary.InnerHtml.Append(Label);
        var explanation = new TagBuilder("p");
        explanation.InnerHtml.Append(Message);
        var button = new TagBuilder("button");
        button.Attributes["type"] = "submit";
        button.AddCssClass(ButtonClass);
        button.InnerHtml.Append("Xác nhận: " + Label.ToLowerInvariant());
        var panel = new TagBuilder("div");
        panel.AddCssClass("confirm-panel");
        panel.InnerHtml.AppendHtml(explanation);
        panel.InnerHtml.AppendHtml(button);
        output.Content.AppendHtml(summary);
        output.Content.AppendHtml(panel);
    }
}
