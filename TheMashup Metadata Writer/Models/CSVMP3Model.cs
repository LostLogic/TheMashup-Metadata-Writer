using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TheMashup_Metadata_Writer.Models;

public class CSVMP3Model
{
    public string FilePath { get; set; }
    public string Artist {  get; set; }
    public string Title { get; set; }
    public string Genre { get; set; }
    public int Year { get; set; }
}
