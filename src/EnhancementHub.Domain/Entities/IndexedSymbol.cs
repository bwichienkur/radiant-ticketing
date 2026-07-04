using EnhancementHub.Domain.Common;

namespace EnhancementHub.Domain.Entities;

public class IndexedSymbol : BaseEntity
{
    public Guid IndexedFileId { get; set; }
    public string SymbolName { get; set; } = string.Empty;
    public string SymbolKind { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public int LineStart { get; set; }
    public int LineEnd { get; set; }

    public IndexedFile IndexedFile { get; set; } = null!;
}
