﻿using System.Windows;
using MahApps.Metro.Controls;

namespace JryVideo.Common.Dialogs
{
    internal static class MessageWindowHelper
    {
        public static void ShowJryVideoMessage(this DependencyObject self, string caption, string message)
        {
            self.TryFindParent<Window>().ShowJryVideoMessage(caption, message);
        }

        public static void ShowJryVideoMessage(this Window self, string caption, string message)
        {
            var dlg = new MessageWindow()
            {
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = self
            };
            dlg.TitleTextBlock.Text = caption;
            dlg.ContentTextBlock.Text = message;
            dlg.ShowDialog();
        }
    }
}