namespace TheMashup_Metadata_Writer.Models;
public class MetadataModel
{
    public List<string> Categories { get; set; } = new List<string>();
    public string Decade { get; set; } = string.Empty;
    public List<string> Genres { get; set; } = new List<string>();
    public string Grouping { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
}

