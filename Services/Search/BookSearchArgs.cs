public record BookSearchArgs(
    string? Title = null,
    string? Description = null,
    bool UseTitle = true,
    bool UseDescription = false,
    string? Author = null,
    int? PublishYear = null,
    string? Publisher = null,
    int? Pages = null,
    string? ISBN = null,
    string? Language = null,
    string? Genre = null);