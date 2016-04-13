using System.ComponentModel;
using System.Runtime.CompilerServices;
using Model.Annotations;

namespace Model.Files
{
    public class BaseWebFile:INotifyPropertyChanged
    {
        private bool _isNotCompleted = true;
        private float _progress;

        public bool IsNotCompleted
        {
            get { return _isNotCompleted; }
            set
            {
                if (value == _isNotCompleted) return;
                _isNotCompleted = value;
                OnPropertyChanged();
            }
        }

        public float Progress
        {
            get { return _progress; }
            set
            {
                if (value.Equals(_progress)) return;
                _progress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(ProgreccPercentString));
            }
        }

        public string ProgreccPercentString
        {
            get { return Progress.ToString("F0")+"%"; }
        }

        public string Path { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
