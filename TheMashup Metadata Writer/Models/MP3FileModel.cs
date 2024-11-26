using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace TheMashup_Metadata_Writer.Models
{
    public class MP3FileModel : INotifyPropertyChanged
    {
        private bool selected;
        private string filePath = string.Empty;
        private string artist = string.Empty;
        private string title = string.Empty;
        private bool tagged;

        public string FilePath
        {
            get => filePath;
            set
            {
                if (filePath != value)
                {
                    filePath = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Artist
        {
            get => artist;
            set
            {
                if (artist != value)
                {
                    artist = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title
        {
            get => title;
            set
            {
                if (title != value)
                {
                    title = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Selected
        {
            get => selected;
            set
            {
                if (selected != value)
                {
                    selected = value;
                    OnPropertyChanged();
                }
            }
        }

        public bool Tagged
        {
            get => tagged;
            set
            {
                if (tagged != value)
                {
                    tagged = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
