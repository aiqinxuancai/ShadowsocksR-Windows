using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Shadowsocks.View.Controls
{
    public static class TreeViewSelectionBehavior
    {
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItem",
                typeof(object),
                typeof(TreeViewSelectionBehavior),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        private static readonly DependencyProperty IsHookedProperty =
            DependencyProperty.RegisterAttached(
                "IsHooked",
                typeof(bool),
                typeof(TreeViewSelectionBehavior),
                new PropertyMetadata(false));

        public static object GetSelectedItem(DependencyObject obj) => obj.GetValue(SelectedItemProperty);

        public static void SetSelectedItem(DependencyObject obj, object value) => obj.SetValue(SelectedItemProperty, value);

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TreeView treeView)
            {
                return;
            }

            if (!(bool)treeView.GetValue(IsHookedProperty))
            {
                treeView.SelectedItemChanged += TreeView_SelectedItemChanged;
                treeView.Unloaded += TreeView_Unloaded;
                treeView.SetValue(IsHookedProperty, true);
            }

            if (e.NewValue != null && !ReferenceEquals(treeView.SelectedItem, e.NewValue))
            {
                treeView.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    SelectItem(treeView, e.NewValue);
                }));
            }
        }

        private static void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            SetSelectedItem((DependencyObject)sender, e.NewValue);
        }

        private static void TreeView_Unloaded(object sender, RoutedEventArgs e)
        {
            if (sender is TreeView treeView)
            {
                treeView.SelectedItemChanged -= TreeView_SelectedItemChanged;
                treeView.Unloaded -= TreeView_Unloaded;
                treeView.ClearValue(IsHookedProperty);
            }
        }

        private static bool SelectItem(ItemsControl parent, object item)
        {
            if (parent == null)
            {
                return false;
            }

            parent.ApplyTemplate();
            parent.UpdateLayout();

            if (parent.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem directItem)
            {
                directItem.IsSelected = true;
                directItem.BringIntoView();
                return true;
            }

            foreach (var child in parent.Items)
            {
                if (parent.ItemContainerGenerator.ContainerFromItem(child) is not TreeViewItem childItem)
                {
                    continue;
                }

                var wasExpanded = childItem.IsExpanded;
                childItem.IsExpanded = true;
                childItem.ApplyTemplate();
                childItem.UpdateLayout();

                if (SelectItem(childItem, item))
                {
                    return true;
                }

                childItem.IsExpanded = wasExpanded;
            }

            return false;
        }
    }
}
