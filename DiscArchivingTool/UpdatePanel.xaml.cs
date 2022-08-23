using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using DiscArchivingTool;
using System.Diagnostics;
using static DiscArchivingTool.CheckUtility;
using FzLib.WPF.Converters;

namespace DiscArchivingTool
{
    /// <summary>
    /// UpdatePanel.xaml 的交互逻辑
    /// </summary>
    public partial class UpdatePanel : UserControl
    {
        UpdateUtility uu = new UpdateUtility();

        public UpdatePanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
        }
        public UpdatePanelViewModel ViewModel { get; } = new UpdatePanelViewModel();



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

        private void BrowseSourceDirButton_Click(object sender, RoutedEventArgs e)
        {

            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.SourceDir = path;
            }
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.InputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("光盘目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.InputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("光盘目录不存在");
                return;
            }
            if (string.IsNullOrWhiteSpace(ViewModel.SourceDir))
            {
                await CommonDialog.ShowErrorDialogAsync("参照目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.SourceDir))
            {
                await CommonDialog.ShowErrorDialogAsync("参照目录不存在");
                return;
            }
            try
            {
                stkConfig.IsEnabled=btnUpdate.IsEnabled = false;
                ViewModel.Message = "正在查找";
                await Task.Run(() =>
                {
                    uu.Search(ViewModel.InputDir, ViewModel.SourceDir, ViewModel.ByName, ViewModel.ByTime, ViewModel.ByLength);
                    ViewModel.UpdatingDiscFiles = uu.UpdatingDiscFiles;
                });
                if(ViewModel.UpdatingDiscFiles.Count>0)
                {
                    btnUpdate.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "查找失败");
            }
            finally
            {
                stkConfig.IsEnabled = true;
                ViewModel.Message = "就绪";
            }
        }

        private async void BtnUpdate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("更新目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("更新目录不存在");
                return;
            }
            try
            {
                stkConfig.IsEnabled = btnUpdate.IsEnabled = false;
                ViewModel.Message = "正在更新";
                await Task.Run(() =>
                {
                    uu.Update(ViewModel.OutputDir);
                });
                btnUpdate.IsEnabled = false;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "更新失败");
            }
            finally
            {
                stkConfig.IsEnabled = true;
                ViewModel.Message = "就绪";
            }
        }
    }


    public class UpdatePanelViewModel : INotifyPropertyChanged
    {
        private bool byLength;
        private bool byName;
        private bool byTime;
        private string inputDir;
        private string message = "就绪";
        private string outputDir;
        private string sourceDir;
        private List<UpdatingDiscFile> updatingDiscFiles;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool ByLength
        {
            get => byLength;
            set => this.SetValueAndNotify(ref byLength, value, nameof(ByLength));
        }

        public bool ByName
        {
            get => byName;
            set => this.SetValueAndNotify(ref byName, value, nameof(ByName));
        }

        public bool ByTime
        {
            get => byTime;
            set => this.SetValueAndNotify(ref byTime, value, nameof(ByTime));
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

        public string SourceDir
        {
            get => sourceDir;
            set => this.SetValueAndNotify(ref sourceDir, value, nameof(SourceDir));
        }

        public List<UpdatingDiscFile> UpdatingDiscFiles
        {
            get => updatingDiscFiles;
            set => this.SetValueAndNotify(ref updatingDiscFiles, value, nameof(UpdatingDiscFiles));
        }
    }

}
