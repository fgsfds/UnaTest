using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Database;

[Table(name: "pdfs", Schema = "main")]
public sealed class PdfsDbEntity
{
    [Key]
    [Column("file_name")]
    public required string FileName { get; set; }

    [Column("content")]
    public required string Content { get; set; }
}
