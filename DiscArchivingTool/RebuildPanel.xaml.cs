using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using DiscArchivingTool;
using System.Diagnostics;

namespace DiscArchivingTool
{
    /// <summary>
    /// RebuildPanel.xaml 的交互逻辑
    /// </summary>
    public partial class RebuildPanel : UserControl
    {
        FileUtility fu = new FileUtility();

        public RebuildPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }
        public RebuildPanelViewModel ViewModel { get; } = new RebuildPanelViewModel();

        private void BrowseInputDirButton_Click(object sender, RoutedEventArgs e)
        {

            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.InputDir = path;
            }
        }

        private void BrowseOutputDirButton_Click(object sender, RoutedEventArgs e)
        {

            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.OutputDir = path;
            }
        }

        private async void BtnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnAnalyze.IsEnabled = btnRebuild.IsEnabled = false;
                ViewModel.Message = "正在重建分析";
                await Task.Run(() =>
                {
                    ViewModel.FileTree = FileUtility.RebuildAnalyze(ViewModel.InputDir);
                });
                btnRebuild.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "解析失败");
            }
            finally
            {
                btnAnalyze.IsEnabled = true;
                ViewModel.Message = "就绪";
            }
        }

        private void BtnRebuild_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TreeViewItem_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var file = (sender as TreeViewItem).DataContext as FreeFileSystemTree;
            if (file != null)
            {
                if (file.IsFile)
                {
                    string path = Path.Combine(ViewModel.InputDir, file.File.DiscName);
                    if (File.Exists(path))
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo(path)
                            {
                                UseShellExecute = true,

                            });
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
        }
    }


    public class RebuildPanelViewModel : INotifyPropertyChanged
    {
        private FreeFileSystemTree fileTree;
        private string inputDir = @"C:\Users\autod\Desktop\test\1";

        private string message = "就绪";

        private string outputDir;

        public event PropertyChangedEventHandler PropertyChanged;

        public FreeFileSystemTree FileTree
        {
            get => fileTree;
            set => this.SetValueAndNotify(ref fileTree, value, nameof(FileTree));
        }
        public string InputDir
        {
            get => inputDir;
            set => this.SetValueAndNotify(ref inputDir, value, nameof(InputDir));
        }
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        public string OutputDir
        {
            get => outputDir;
            set => this.SetValueAndNotify(ref outputDir, value, nameof(OutputDir));
        }
    }

}
