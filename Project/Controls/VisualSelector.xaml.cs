using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace Kazyx.WPPMM.Controls
{
    public partial class VisualSelector : UserControl
    {
        public VisualSelector()
        {
            InitializeComponent();
        }

        public event SelectionEventHandler Selected;

        public delegate void SelectionEventHandler(object sender, SelectionEventArgs e);

        protected void OnSelected(SelectorItem item)
        {
            if (Selected != null)
            {
                Selected(this, new SelectionEventArgs(item));
            }
        }

        private void ItemImage_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            var img = sender as Button;
            if (img == null)
            {
                return;
            }
            var item = img.DataContext as SelectorItem;
            if (item == null)
            {
                return;
            }
            OnSelected(item);
        }

        private void ItemTitle_Tap(object sender, System.Windows.Input.GestureEventArgs e)
        {
            ButtonSelected(sender);
        }

        private void ItemImage_Click(object sender, RoutedEventArgs e)
        {
            ButtonSelected(sender);
        }

        private void ButtonSelected(object sender)
        {
            var text = sender as TextBlock;
            if (text == null)
            {
                return;
            }
            var item = text.DataContext as SelectorItem;
            if (item == null)
            {
                return;
            }
            OnSelected(item);
        }
    }

    public class SelectorItem : INotifyPropertyChanged
    {
        public SelectorItem(string id)
        {
            this.Id = id;
        }

        private string _Title;
        public string Title
        {
            set
            {
                _Title = value;
                OnPropertyChanged("Title");
                OnPropertyChanged("TitleVisibility");
            }
            get { return _Title; }
        }
        public Visibility TitleVisibility
        {
            get { return (string.IsNullOrEmpty(Title)) ? Visibility.Collapsed : Visibility.Visible; }
        }

        private BitmapImage _Image;
        public BitmapImage Image
        {
            set
            {
                _Image = value;
                OnPropertyChanged("Image");
            }
            get
            {
                return _Image;
            }
        }

        public readonly string Id;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                }
            }
        }
    }

    public class SelectionEventArgs : EventArgs
    {
        public SelectorItem Item { private set; get; }

        internal SelectionEventArgs(SelectorItem item)
        {
            Item = item;
        }
    }

    public class ItemGroup : INotifyPropertyChanged
    {
        ObservableCollection<SelectorItem> _Group = new ObservableCollection<SelectorItem>();

        public ObservableCollection<SelectorItem> Group
        {
            set
            {
                _Group = value;
                OnPropertyChanged("Group");
            }
            get { return _Group; }
        }

        public void Add(SelectorItem data)
        {
            _Group.Add(data);
            OnPropertyChanged("Group");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            //Debug.WriteLine("OnPropertyChanged: " + name);
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                }
            }
        }
    }
}
