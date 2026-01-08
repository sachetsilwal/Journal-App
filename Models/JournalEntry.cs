using System.ComponentModel.DataAnnotations;

namespace Journal.Models;

public class JournalEntry
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public DateOnly EntryDate { get; set; } // One entry per day rule (enforced also via DB unique index)

    [Required, MaxLength(200)]
    public string Title { get; set; } = "Untitled";

    [Required]
    public string ContentMarkdown { get; set; } = string.Empty;

    [Required]
    public Mood PrimaryMood { get; set; }

    public Mood? SecondaryMood1 { get; set; }
    public Mood? SecondaryMood2 { get; set; }

    // Simple storage for coursework: CSV tags + category
    public string Category { get; set; } = "General";
    public string TagsCsv { get; set; } = string.Empty;

    // System-generated timestamps
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
