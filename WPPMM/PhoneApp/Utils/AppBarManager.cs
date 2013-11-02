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

        readonly Dictionary<Menu, ApplicationBarMenuItem> MenuItems = new Dictionary<Menu, ApplicationBarMenuItem>();
        readonly Dictionary<IconMenu, ApplicationBarIconButton> IconMenuItems = new Dictionary<IconMenu, ApplicationBarIconButton>();

        readonly SortedSet<Menu> EnabledItems = new SortedSet<Menu>();
        readonly SortedSet<IconMenu> EnabledIconItems = new SortedSet<IconMenu>();

        public AppBarManager()
        {
            MenuItems.Add(Menu.About, AboutMenuItem);
            MenuItems.Add(Menu.ImageSize, PostViewMenuItem);
        }

        public void SetEvent(Menu type, EventHandler handler)
        {
            MenuItems[type].Click += handler;
        }

        public void SetEvent(IconMenu type, EventHandler handler)
        {
            IconMenuItems[type].Click += handler;
        }

        public void JustClear()
        {
            EnabledItems.Clear();
            EnabledIconItems.Clear();
        }

        public IApplicationBar Clear()
        {
            JustClear();
            return SetEnabledItems();
        }

        public bool IsEnabled(Menu type)
        {
            return EnabledItems.Contains(type);
        }

        public IApplicationBar Enable(Menu type)
        {
            if (!EnabledItems.Contains(type))
            {
                EnabledItems.Add(type);
            }
            return SetEnabledItems();
        }

        public IApplicationBar Disable(Menu type)
        {
            if (EnabledItems.Contains(type))
            {
                EnabledItems.Remove(type);
            }
            return SetEnabledItems();
        }

        public bool IsEnabled(IconMenu type)
        {
            return EnabledIconItems.Contains(type);
        }

        public IApplicationBar Enable(IconMenu type)
        {
            if (!EnabledIconItems.Contains(type))
            {
                EnabledIconItems.Add(type);
            }
            return SetEnabledItems();
        }

        public IApplicationBar Disable(IconMenu type)
        {
            if (EnabledIconItems.Contains(type))
            {
                EnabledIconItems.Remove(type);
            }
            return SetEnabledItems();
        }

        private IApplicationBar SetEnabledItems()
        {
            var bar = new ApplicationBar();
            bar.Mode = ApplicationBarMode.Minimized;
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

    }
}
