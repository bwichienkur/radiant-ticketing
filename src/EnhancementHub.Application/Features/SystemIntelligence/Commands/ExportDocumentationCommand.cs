using EnhancementHub.Application.Abstractions;
using EnhancementHub.Application.Features.SystemIntelligence.Dtos;
using EnhancementHub.Domain.Enums;
using MediatR;

namespace EnhancementHub.Application.Features.SystemIntelligence.Commands;

public sealed record ExportDocumentationCommand(
    Guid ApplicationId,
    DocumentationExportFormat Format) : IRequest<DocumentationExportResultDto>;

public sealed class ExportDocumentationCommandHandler
    : IRequestHandler<ExportDocumentationCommand, DocumentationExportResultDto>
{
    private readonly IDocumentationExportService _exportService;

    public ExportDocumentationCommandHandler(IDocumentationExportService exportService) =>
        _exportService = exportService;

    public async Task<DocumentationExportResultDto> Handle(
        ExportDocumentationCommand request,
        CancellationToken cancellationToken)
    {
        var bundle = await _exportService.ExportAsync(request.ApplicationId, cancellationToken);

        var content = request.Format switch
        {
            DocumentationExportFormat.Mermaid => bundle.MermaidErd,
            DocumentationExportFormat.Both => $"{bundle.MarkdownDocumentation}\n\n## ERD\n\n```mermaid\n{bundle.MermaidErd}\n```",
            _ => bundle.MarkdownDocumentation
        };

        var extension = request.Format switch
        {
            DocumentationExportFormat.Mermaid => "mmd",
            DocumentationExportFormat.Both => "md",
            _ => "md"
        };

        return new DocumentationExportResultDto(
            request.ApplicationId,
            content,
            request.Format == DocumentationExportFormat.Mermaid ? "text/plain" : "text/markdown",
            $"system-docs-{request.ApplicationId:N}.{extension}");
    }
}
