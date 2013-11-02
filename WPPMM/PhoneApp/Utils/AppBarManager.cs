using Microsoft.Phone.Shell;
using System;
using System.Collections.Generic;
using WPPMM.Resources;

namespace WPPMM.Utils
{
    public class AppBarManager
    {
        readonly ApplicationBarMenuItem AboutMenuItem = new ApplicationBarMenuItem(AppResources.About);
        readonly ApplicationBarMenuItem PostViewMenuItem = new ApplicationBarMenuItem(AppResources.Setting_PostViewImageSize);

        readonly ApplicationBarIconButton WifiMenuItem = new ApplicationBarIconButton
        {
            Text = AppResources.WifiSettingLauncherButtonText,
            IconUri = new Uri("/Assets/AppBar/feature.settings.png", UriKind.Relative)
        };

        readonly Dictionary<Menu, ApplicationBarMenuItem> MenuItems = new Dictionary<Menu, ApplicationBarMenuItem>();
        readonly Dictionary<IconMenu, ApplicationBarIconButton> IconMenuItems = new Dictionary<IconMenu, ApplicationBarIconButton>();

        readonly SortedSet<Menu> EnabledItems = new SortedSet<Menu>();
        readonly SortedSet<IconMenu> EnabledIconItems = new SortedSet<IconMenu>();

        public AppBarManager()
        {
            MenuItems.Add(Menu.About, AboutMenuItem);
            MenuItems.Add(Menu.ImageSize, PostViewMenuItem);
            IconMenuItems.Add(IconMenu.WiFi, WifiMenuItem);
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

        public IApplicationBar CreateNew()
        {
            var bar = new ApplicationBar();

            if (EnabledIconItems.Count == 0)
                bar.Mode = ApplicationBarMode.Minimized;
            else
                bar.Mode = ApplicationBarMode.Default;

            bar.Opacity = 0.5;

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
        About,
        ImageSize
    }

    public enum IconMenu
    {
        WiFi
    }
}
