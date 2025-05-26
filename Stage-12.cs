@using System.IO
@using Markdig
@inject IWebHostEnvironment Env

<div class="accordion my-3" id="markdownAccordion">
    <div class="accordion-item">
        <h2 class="accordion-header" id="headingMarkdown">
            <button class="accordion-button collapsed" type="button" data-bs-toggle="collapse" data-bs-target="#collapseMarkdown" aria-expanded="false" aria-controls="collapseMarkdown">
                Показать Markdown
            </button>
        </h2>
        <div id="collapseMarkdown" class="accordion-collapse collapse" aria-labelledby="headingMarkdown" data-bs-parent="#markdownAccordion">
            <div class="accordion-body">
                @if (string.IsNullOrEmpty(HtmlContent))
                {
                    <p><em>Загрузка...</em></p>
                }
                else
                {
                    @((MarkupString)HtmlContent)
                }
            </div>
        </div>
    </div>
</div>

@code {
    private string HtmlContent;

    protected override async Task OnInitializedAsync()
    {
        var path = Path.Combine(Env.WebRootPath, "content.md");
        if (File.Exists(path))
        {
            var markdown = await File.ReadAllTextAsync(path);
            var pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            HtmlContent = Markdown.ToHtml(markdown, pipeline);
        }
    }
}
