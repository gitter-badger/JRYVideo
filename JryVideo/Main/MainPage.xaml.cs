﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using JryVideo.Add;
using JryVideo.Common;
using JryVideo.Core.Managers;
using JryVideo.Editors.CoverEditor;
using MahApps.Metro.Controls;

namespace JryVideo.Main
{
    /// <summary>
    /// MainPage.xaml 的交互逻辑
    /// </summary>
    public partial class MainPage : Page
    {
        public event EventHandler<VideoInfoViewModel> VideoSelected;

        private MainViewModel ViewModel;

        public MainPage()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// 引发 <see cref="E:System.Windows.FrameworkElement.Initialized"/> 事件。 每当在内部将 <see cref="P:System.Windows.FrameworkElement.IsInitialized"/> 设置为 true 时，都将调用此方法。
        /// </summary>
        /// <param name="e">包含事件数据的 <see cref="T:System.Windows.RoutedEventArgs"/>。</param>
        protected override async void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);
            
            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                await JryVideo.Core.JryVideoCore.Current.InitializeAsync();
                this.DataContext = this.ViewModel = new MainViewModel();
                await this.ViewModel.LoadAsync();
            }
        }

        private async void EditCover_OnClick(object sender, RoutedEventArgs e)
        {
            var frameworkElement = sender as FrameworkElement;
            if (frameworkElement == null) return;

            var vm = frameworkElement.DataContext as VideoInfoViewModel;
            if (vm == null) return;

            var cover = await vm.TryGetCoverAsync();
            if (cover != null)
            {
                var dlg = new CoverEditorWindow();
                dlg.ViewModel.ModifyMode(cover);
                dlg.UpdateRadioButtonCheckedStatus();

                if (dlg.ShowDialog() == true)
                {
                    await dlg.ViewModel.CommitAsync();
                    vm.BeginUpdateCover();
                }
            }
        }

        private async void AddMenuItem_OnClick(object sender, RoutedEventArgs e)
        {
            var selectSeriesWindow = new AddWindow();
            selectSeriesWindow.ShowDialog();
            await this.ViewModel.LoadAsync();
        }

        private void VideoPanel_OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var vm = ((FrameworkElement)sender).DataContext as VideoInfoViewModel;
            if (vm != null) this.VideoSelected.Fire(this, vm);
        }

        private async void SearchTextBox_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await this.ViewModel.VideosViewModel.SearchAsync();
            }
        }
    }
}