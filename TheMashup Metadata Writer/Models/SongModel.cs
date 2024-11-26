namespace TheMashup_Metadata_Writer.Models;

public class SongModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string[] Genre { get; set; } = [];
    public string[] Classifications { get; set; } = [];
    public string[] TheMashupClassification { get; set; } = [];
    public int BPM { get; set; } = 0;
    public double Rating { get; set; } = 0.0;
    public DateTime Uploaded { get; set; } = DateTime.Now;
    public string GenreString => Genre != null ? string.Join(", ", Genre) : string.Empty;
    public string ClassificationsString => Classifications != null ? string.Join(", ", Classifications) : string.Empty;
    public string TheMashupClassificationString => TheMashupClassification != null ? string.Join(", ", TheMashupClassification) : string.Empty;
}



