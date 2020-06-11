using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace NexusLabs.Collections.Generic
{
    public class ReadOnlyBulkObservableCollection<T> :
      ReadOnlyCollection<T>,
      INotifyCollectionChanged,
      INotifyPropertyChanged
    {
        public ReadOnlyBulkObservableCollection(BulkObservableCollection<T> collection)
            : base(collection)
        {
            collection.CollectionChanged += HandleCollectionChanged;
            collection.PropertyChanged += HandlePropertyChanged;
        }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }

        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChanged?.Invoke(this, e);
        }

        private void HandleCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnCollectionChanged(e);
        }

        private void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            OnPropertyChanged(e);
        }
    }
}
