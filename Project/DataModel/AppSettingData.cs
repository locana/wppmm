using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;

namespace Kazyx.WPPMM.DataModel
{
    public class AppSettingData<T> : INotifyPropertyChanged
    {
        public AppSettingData(string title, string guide, Func<T> StateChecker, Action<T> StateChanger, string[] candidates = null)
        {
            if (StateChecker == null || StateChanger == null)
            {
                throw new ArgumentNullException("StateChecker must not be null");
            }
            Title = title;
            Guide = guide;
            Candidates = candidates;
            this.StateChecker = StateChecker;
            this.StateChanger = StateChanger;
        }

        private string _Title = null;
        public string Title
        {
            set
            {
                _Title = value;
                OnPropertyChanged("Title");
            }
            get { return _Title; }
        }

        private string _Guide = null;
        public string Guide
        {
            set
            {
                _Guide = value;
                OnPropertyChanged("Guide");
                OnPropertyChanged("GuideVisibility");
            }
            get { return _Guide; }
        }

        private string[] _Candidates = null;
        public string[] Candidates
        {
            set
            {
                _Candidates = value;
                OnPropertyChanged("Candidates");
            }
            get { return _Candidates; }
        }

        public Visibility GuideVisibility
        {
            get { return Guide == null ? Visibility.Collapsed : Visibility.Visible; }
        }

        private readonly Func<T> StateChecker;
        private readonly Action<T> StateChanger;

        public T CurrentSetting
        {
            get { return StateChecker(); }
            set
            {
                StateChanger.Invoke(value);
                OnPropertyChanged("CurrentSetting");
            }
        }

        private bool _IsActive = true;
        public bool IsActive
        {
            get
            {
                return _IsActive;
            }
            set
            {
                _IsActive = value;
                OnPropertyChanged("IsActive");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                try
                {
                    PropertyChanged(this, new PropertyChangedEventArgs(name));
                }
                catch (COMException)
                {
                    Debug.WriteLine("Caught COMException: AppSettingData");
                }
            }
        }
    }
}
