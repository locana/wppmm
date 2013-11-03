using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using WPPMM.Resources;

namespace WPPMM.Utils
{
    public class AppBarManager
    {
        readonly ApplicationBarMenuItem PostViewMenuItem = new ApplicationBarMenuItem(AppResources.Setting_PostViewImageSize);

        readonly ApplicationBarIconButton WifiMenuItem = new ApplicationBarIconButton
        {
            Text = AppResources.WifiSettingLauncherButtonText,
            IconUri = new Uri("/Assets/AppBar/feature.settings.png", UriKind.Relative)
        };

        readonly ApplicationBarIconButton AboutMenuItem = new ApplicationBarIconButton
        {
            Text = AppResources.About,
            IconUri = new Uri("/Assets/AppBar/questionmark.png", UriKind.Relative)
        };

        readonly ApplicationBarIconButton SwitchShootModeItem = new ApplicationBarIconButton
        {
            Text = AppResources.Switch,
            IconUri = new Uri("/Assets/AppBar/still_movie.png", UriKind.Relative)
        };

        readonly ApplicationBarIconButton SelfTimerItem = new ApplicationBarIconButton
        {
            Text = AppResources.SelfTimer,
            IconUri = new Uri("/Assets/AppBar/feature.alarm.png", UriKind.Relative)
        };

        readonly Dictionary<Menu, ApplicationBarMenuItem> MenuItems = new Dictionary<Menu, ApplicationBarMenuItem>();
        readonly Dictionary<IconMenu, ApplicationBarIconButton> IconMenuItems = new Dictionary<IconMenu, ApplicationBarIconButton>();

        readonly SortedSet<Menu> EnabledItems = new SortedSet<Menu>();
        readonly SortedSet<IconMenu> EnabledIconItems = new SortedSet<IconMenu>();

        public AppBarManager()
        {
            MenuItems.Add(Menu.ImageSize, PostViewMenuItem);
            IconMenuItems.Add(IconMenu.WiFi, WifiMenuItem);
            IconMenuItems.Add(IconMenu.About, AboutMenuItem);
            IconMenuItems.Add(IconMenu.SwitchShootMode, SwitchShootModeItem);
            IconMenuItems.Add(IconMenu.SelfTimer, SelfTimerItem);
        }

        public AppBarManager SetEvent(Menu type, EventHandler handler)
        {
            MenuItems[type].Click += handler;
            return this;
        }

        public AppBarManager SetEvent(IconMenu type, EventHandler handler)
        {
            IconMenuItems[type].Click += handler;
            return this;
        }

        public AppBarManager Clear()
        {
            EnabledItems.Clear();
            EnabledIconItems.Clear();
            return this;
        }

        public bool IsEnabled(Menu type)
        {
            return EnabledItems.Contains(type);
        }

        public AppBarManager Enable(Menu type)
        {
            if (!EnabledItems.Contains(type))
            {
                EnabledItems.Add(type);
            }
            return this;
        }

        public AppBarManager Disable(Menu type)
        {
            if (EnabledItems.Contains(type))
            {
                EnabledItems.Remove(type);
            }
            return this;
        }

        public bool IsEnabled(IconMenu type)
        {
            return EnabledIconItems.Contains(type);
        }

        public AppBarManager Enable(IconMenu type)
        {
            if (!EnabledIconItems.Contains(type))
            {
                EnabledIconItems.Add(type);
            }
            return this;
        }

        public AppBarManager Disable(IconMenu type)
        {
            if (EnabledIconItems.Contains(type))
            {
                EnabledIconItems.Remove(type);
            }
            return this;
        }

        public IApplicationBar CreateNew(double opacity)
        {
            var bar = new ApplicationBar();

            if (EnabledIconItems.Count == 0)
                if (EnabledItems.Count == 0)
                    return null;
                else
                    bar.Mode = ApplicationBarMode.Minimized;
            else
                bar.Mode = ApplicationBarMode.Default;

            bar.Opacity = opacity;

            foreach (Menu menu in EnabledItems)
            {
                bar.MenuItems.Add(MenuItems[menu]);
            }
            foreach (IconMenu menu in EnabledIconItems)
            {
                bar.Buttons.Add(IconMenuItems[menu]);
            }
            return bar;
        }
    }

    public enum Menu
    {
        ImageSize
    }

    public enum IconMenu
    {
        WiFi,
        About,
        SwitchShootMode,
        SelfTimer
    }
}
