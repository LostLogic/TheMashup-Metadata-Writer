namespace TheMashup_Metadata_Writer.Helpers;

[Serializable]
public class SerializableCookie
{
    public string Name { get; set; }
    public string Value { get; set; }
    public string Path { get; set; } = "/";
    public string Domain { get; set; }
    public DateTime? Expires { get; set; }
    public bool Secure { get; set; }
    public bool HttpOnly { get; set; }
}

