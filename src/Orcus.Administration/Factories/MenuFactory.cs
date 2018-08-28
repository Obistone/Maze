﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Orcus.Administration.Library.Menu;
using Orcus.Administration.Library.Menu.Internal;
using Orcus.Administration.Library.Menu.MenuBase;

namespace Orcus.Administration.Factories
{
    public class DefaultMenuFactory : IMenuFactory
    {
        public IEnumerable<UIElement> Create<TCommand>(IEnumerable<IMenuEntry<TCommand>> menuEntries, object context) where TCommand : ICommandMenuEntry
        {
            return CreateInternal(menuEntries, context);
        }

        private static IReadOnlyList<UIElement> CreateInternal<TCommand>(IEnumerable<IMenuEntry<TCommand>> menuEntries, object context) where TCommand : ICommandMenuEntry
        {
            var result = new List<UIElement>();

            foreach (var menuEntry in menuEntries)
                if (menuEntry is MenuSection<TCommand> section)
                {
                    var items = CreateInternal(section, context);

                    if (!items.Any())
                        continue;

                    foreach (var menuItem in items)
                        result.Add(menuItem);

                    result.Add(CreateSeparator());
                }
                else if (menuEntry is NavigationalEntry<TCommand> navigationalEntry)
                {
                    var items = CreateInternal(navigationalEntry, context);

                    if (!items.Any())
                        continue;

                    var menuItem = CreateMenuItem(navigationalEntry);

                    foreach (var item in items)
                        menuItem.Items.Add(item);
                    result.Add(menuItem);
                }
                else if (menuEntry is CommandWrapper<TCommand> commandWrapper)
                {
                    result.Add(CreateCommandMenuItem(commandWrapper.CommandEntry, context));
                }

            //dont finish with separator
            if (result.Any() && result.Last() is Separator)
                result.RemoveAt(result.Count - 1);

            return result;
        }

        private static Separator CreateSeparator()
        {
            return new Separator();
        }

        private static MenuItem CreateMenuItem(IVisibleMenuItem menuItemInfo)
        {
            return new MenuItem {Header = menuItemInfo.Header, Icon = menuItemInfo.Icon};
        }

        private static MenuItem CreateCommandMenuItem(ICommandMenuEntry menuItemInfo, object context)
        {
            var menuItem = CreateMenuItem(menuItemInfo);
            menuItem.Command = menuItemInfo.Command;

            if (menuItem.Command is IContextAwareCommand contextAwareCommand)
                contextAwareCommand.Context = context;

            return menuItem;
        }
    }

    public class ItemMenuFactory : IItemMenuFactory
    {
        public IEnumerable<UIElement> Create<TCommand>(IEnumerable<IMenuEntry<TCommand>> menuEntries, object context) where TCommand : IItemCommandMenuEntry
        {
            var (result, _, _) = CreateInternal(menuEntries, context);
            return result;
        }

        private static (IReadOnlyList<UIElement>, bool visibleForSingleItem, bool visibleForMultipleItems)  CreateInternal<TCommand>(IEnumerable<IMenuEntry<TCommand>> menuEntries, object context) where TCommand : IItemCommandMenuEntry
        {
            var result = new List<UIElement>();
            var forSingleItem = false;
            var forMultipleItems = false;

            foreach (var menuEntry in menuEntries)
                if (menuEntry is MenuSection<TCommand> section)
                {
                    var (items, singleItem, multipleItems) = CreateInternal(section, context);

                    if (!items.Any())
                        continue;

                    if (singleItem)
                        forSingleItem = true;
                    if (multipleItems)
                        forMultipleItems = true;

                    foreach (var menuItem in items)
                        result.Add(menuItem);

                    result.Add(CreateSeparator(singleItem, multipleItems));
                }
                else if (menuEntry is NavigationalEntry<TCommand> navigationalEntry)
                {
                    var (items, singleItem, multipleItems) = CreateInternal(navigationalEntry, context);

                    if (!items.Any())
                        continue;

                    if (singleItem)
                        forSingleItem = true;
                    if (multipleItems)
                        forMultipleItems = true;

                    var menuItem = CreateMenuItem(navigationalEntry, singleItem, multipleItems);

                    foreach (var item in items)
                        menuItem.Items.Add(item);
                    result.Add(menuItem);
                }
                else if (menuEntry is CommandWrapper<TCommand> commandWrapper)
                {
                    if (commandWrapper.CommandEntry.SingleItemCommand != null)
                        forSingleItem = true;
                    if (commandWrapper.CommandEntry.MultipleItemsCommand != null)
                        forMultipleItems = true;

                    result.Add(CreateCommandMenuItem(commandWrapper.CommandEntry, context));
                }

            //dont finish with separator
            if (result.Any() && result.Last() is Separator)
                result.RemoveAt(result.Count - 1);

            return (result, forSingleItem, forMultipleItems);
        }

        private static Separator CreateSeparator(bool visibleForSingle, bool visibleForMultiple)
        {
            var separator = new Separator();

            string styleName;
            if (visibleForSingle && !visibleForMultiple)
                styleName = "SeparatorVisibleForSingleSelection";
            else if (!visibleForSingle && visibleForMultiple)
                styleName = "SeparatorVisibleForMultipleSelection";
            else return separator;

            separator.Style = (Style) Application.Current.FindResource(styleName);
            return separator;
        }

        private static MenuItem CreateMenuItem(IVisibleMenuItem menuItemInfo, bool visibleForSingle,
            bool visibleForMultiple)
        {
            var menuItem = new MenuItem {Header = menuItemInfo.Header, Icon = menuItemInfo.Icon};

            string styleName;
            if (visibleForSingle && !visibleForMultiple)
                styleName = "MenuItemVisibleForSingleSelection";
            else if (!visibleForSingle && visibleForMultiple)
                styleName = "MenuItemVisibleForMultipleSelection";
            else styleName = "MenuItemVisibleEverything";

            menuItem.Style = (Style) Application.Current.FindResource(styleName);
            return menuItem;
        }

        private static MenuItem CreateCommandMenuItem(IItemCommandMenuEntry menuItemInfo, object context)
        {
            var menuItem = CreateMenuItem(menuItemInfo, menuItemInfo.SingleItemCommand != null,
                menuItemInfo.MultipleItemsCommand != null);

            menuItem.Command = menuItemInfo.Command;

            if (menuItem.Command is IContextAwareCommand contextAwareCommand)
                contextAwareCommand.Context = context;

            return menuItem;
        }
    }
}