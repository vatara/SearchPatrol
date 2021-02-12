using System;
using System.ComponentModel;

namespace SearchPatrol.Wpf
{
    public class RoundedProp : INotifyPropertyChanged
    {
        private double value;

        private readonly int digits;

        public RoundedProp(int digits)
        {
            this.digits = digits;
        }

        public double Value
        {
            get => value;
            set { this.value = Math.Round(value, digits); NotifyPropertyChanged(nameof(Value)); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        internal void NotifyPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
