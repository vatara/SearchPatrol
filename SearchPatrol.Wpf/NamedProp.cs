using System.ComponentModel;

namespace SearchPatrol.Wpf
{
    public class NamedProp<T> : INotifyPropertyChanged
    {
        private T _value;

        public T Value
        {
            get => _value;
            set { _value = value; NotifyPropertyChanged(nameof(Value)); }
        }

        public string Name { get; }

        public NamedProp(string name)
        {
            Name = name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
