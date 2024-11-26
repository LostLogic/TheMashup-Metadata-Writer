using TheMashup_Metadata_Writer.Models;

namespace TheMashup_Metadata_Writer.Helpers
{
    public static class MetadataMaster
    {
        public static string WriteMetadata(SongModel song, MP3FileModel mp3File)
        {
            if (mp3File == null || song == null)
            {
                return "Failed: File or Song data missing";
            }

            try
            {
                using var tagFile = TagLib.File.Create(mp3File.FilePath);
                var metadata = GetMetadata(CollectSongMetadata(song), song.Title);

                SetBasicMetadata(tagFile, metadata);
                SetCustomMetadata(tagFile, metadata, song);

                tagFile.Save();
                tagFile.Dispose();
                return "Success: Metadata saved successfully";
            }
            catch (Exception ex)
            {
                return $"Failed: Metadata saving failed: {ex.Message}";
            }
        }

        private static List<string> CollectSongMetadata(SongModel song)
        {
            var songMetadata = new List<string>();

            if (song.Classifications != null)
            {
                songMetadata.AddRange(song.Classifications);
            }

            if (song.Genre != null)
            {
                songMetadata.AddRange(song.Genre);
            }

            if (song.TheMashupClassification != null)
            {
                songMetadata.AddRange(song.TheMashupClassification);
            }

            return new HashSet<string>(songMetadata).ToList();
        }

        private static void SetBasicMetadata(TagLib.File tagFile, MetadataModel metadata)
        {
            tagFile.Tag.Genres = [string.Join(", ", metadata.Genres)];
            tagFile.Tag.Grouping = metadata.Grouping;
            tagFile.Tag.Comment = string.Join(", ", metadata.Categories);

            if (!string.IsNullOrEmpty(metadata.Decade))
            {
                tagFile.Tag.Comment += ", " + metadata.Decade;
            }
        }

        private static void SetCustomMetadata(TagLib.File tagFile, MetadataModel metadata, SongModel song)
        {
            SetAdvisoryFrame(tagFile, metadata.Version);
            tagFile.Tag.Publisher = metadata.Version;
            tagFile.Tag.BeatsPerMinute = (uint)song.BPM;
            SetCustomIdTag(tagFile, song.Id);
            SetRating(tagFile, song.Rating);
        }

        private static void SetAdvisoryFrame(TagLib.File tagFile, string version)
        {
            var advisoryFrame = TagLib.Id3v2.UserTextInformationFrame.Get((TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, true), "ITUNESADVISORY", true);
            if (!string.IsNullOrEmpty(version))
            {
                advisoryFrame.Text = [version.Contains("dirty", StringComparison.InvariantCultureIgnoreCase) ? "1" : "2"];
            }
            else
            {
                advisoryFrame.Text = ["0"];
            }
        }

        private static void SetCustomIdTag(TagLib.File tagFile, int id)
        {
            var id3v2TagMashupId = TagLib.Id3v2.UserTextInformationFrame.Get((TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, true), "TheMashupID", true);
            id3v2TagMashupId.Text = [id.ToString()];
        }

        private static void SetRating(TagLib.File tagFile, double rating)
        {
            var scaledRating = rating == 0 ? (byte)1 : (byte)(rating * 51);
            var popmFrame = TagLib.Id3v2.PopularimeterFrame.Get((TagLib.Id3v2.Tag)tagFile.GetTag(TagLib.TagTypes.Id3v2, true), "TheMashup.co.uk", true);
            popmFrame.Rating = scaledRating;
        }

        public static MetadataModel GetMetadata(List<string> metadataCollection, string songTitle)
        {
            var metadata = new MetadataModel()
            {
                Categories = GetCategories(metadataCollection),
                Decade = GetDecades(metadataCollection),
                Genres = GetGenres(metadataCollection),
                Version = GetVersion(songTitle),
                Grouping = GetGrouping(metadataCollection)
            };

            return metadata;
        }

        public static List<CSVMP3Model> GetMetadataCSV(List<string> files)
        {
            var csvMp3List = new List<CSVMP3Model>();

            foreach (var file in files)
            {
                if(File.Exists(file))
                {
                    using var tagFile = TagLib.File.Create(file);

                    csvMp3List.Add(new CSVMP3Model()
                    {
                        FilePath = file,
                        Artist = string.Join(", ", tagFile.Tag.Performers),
                        Title = tagFile.Tag.Title,
                        Genre = string.Join(", ", tagFile.Tag.Genres),
                        Year = Convert.ToInt32(tagFile.Tag.Year)
                    });
                }
            }

            return csvMp3List;
        }

        public static bool WriteMetadataCSV(List<CSVMP3Model> files)
        {
            foreach(var file in files)
            {
                if(File.Exists(file.FilePath))
                {
                    using var tagFile = TagLib.File.Create(file.FilePath);

                    tagFile.Tag.Performers = file.Artist.Split(", ");
                    tagFile.Tag.Genres = file.Genre.Split(", ");
                    tagFile.Tag.Year = Convert.ToUInt32(file.Year);
                    tagFile.Tag.Title = file.Title;

                    tagFile.Save();
                    tagFile.Dispose();
                }
            }
            
            return true;
        }

        public static List<string> GetGenres(List<string> genres)
        {
            return genres.Intersect(_genres).ToList();
        }

        public static List<string> GetCategories(List<string> categories)
        {
            return categories.Intersect(_categories).ToList();
        }

        public static string GetGrouping(List<string> groupings)
        {
            return groupings.Intersect(_groupings).First();
        }

        public static string GetVersion(string title)
        {
            if (title.Contains("(clean)", StringComparison.OrdinalIgnoreCase))
            {
                return "Clean";
            }

            if (title.Contains("(dirty)", StringComparison.OrdinalIgnoreCase))
            {
                return "Dirty";
            }

            return "";
        }

        public static string GetDecades(List<string> decades)
        {
            string? decade = decades.Intersect(_decades).FirstOrDefault();
            if (decade != null)
            {
                return decade;
            }
            return "";
        }

        private static readonly List<string> _groupings =
        [
            "Commercial",
            "Urban",
            "Club"
        ];

        private static readonly List<string> _categories =
        [
            "Acapella Intros",
            "Acapella Outros",
            "Acapellas",
            "Flip Edits",
            "Hype Intros",
            "Instrumentals",
            "Latin",
            "Mashups",
            "Party/Freshers",
            "Quantized",
            "Short Edits",
            "Strings",
            "Throwback Intros",
            "TMU Intros",
            "Transitions",
            "UK Urban Intros",
            "Urban Intros",
            "Wordplays",
            "Edit",
            "End of Night",
            "Mashup",
            "Original",
            "Producer Countdown",
            "Remix",
            "Sped Up / Slowed Down",
            "Throwback",
            "Toilet Break"
        ];

        private static readonly List<string> _decades =
        [
            "00's",
            "10's",
            "20's",
            "50's",
            "60's",
            "70's",
            "80's",
            "90's"
        ];

        private static readonly List<string> _genres =
        [
            "Afro House",
            "Afrobeats",
            "Amapiano",
            "Balie Funk",
            "Bassline",
            "Bounce",
            "Chillout",
            "Club Classic",
            "Country",
            "Dancehall/Reggae",
            "Deep House",
            "Disco",
            "Disco House",
            "Drill",
            "Drum and Bass",
            "Dubstep",
            "EDM",
            "Funk",
            "Funky House",
            "Future House",
            "FX",
            "Garage",
            "Grime",
            "Hard Dance",
            "Hard Groove",
            "Hip Hop",
            "House",
            "Hypertechno",
            "Indie",
            "Indie Dance/Nu Disco",
            "International",
            "Jungle",
            "K-Pop",
            "Latin",
            "Moombahton",
            "Motown",
            "Piano House",
            "Pop",
            "Pop & Party",
            "R&B",
            "Reggaeton",
            "Rock",
            "Soul",
            "Tech House",
            "Techno",
            "Trance",
            "Trap"
        ];
    }
}
