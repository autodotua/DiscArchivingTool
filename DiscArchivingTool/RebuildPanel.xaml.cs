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
        RebuildUtility ru = new RebuildUtility();

        public RebuildPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
            ru.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };
            ru.RebuildProgressUpdated += (s, e) =>
            {
                if (e.MaxValue != ViewModel.ProgressMax)
                {
                    ViewModel.ProgressMax = e.MaxValue;
                }
                ViewModel.Progress = e.Value;
            };
        }
        public RebuildPanelViewModel ViewModel { get; } = new RebuildPanelViewModel();

        private void BrowseInputDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FileFilterCollection().CreateOpenFileDialog();
            dialog.Multiselect = true;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                ViewModel.InputDir = string.Join('|', dialog.FileNames);
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
            try
            {
                btnAnalyze.IsEnabled = btnRebuild.IsEnabled = false;
                ViewModel.Message = "正在重建分析";
                await Task.Run(() =>
                {
                    ru.InitFileList(ViewModel.InputDir);
                    ViewModel.FileTree = ru.BuildTree();
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

        private async void BtnRebuild_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("重建目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("重建目录不存在");
                return;
            }
            if(ViewModel.FileTree.Count==0)
            {
                await CommonDialog.ShowErrorDialogAsync("没有任何需要重建的文件");
                return;
            }
            try
            {
                ViewModel.RebuildErrors.Clear();
                stkConfig.IsEnabled = false;
                btnStop.IsEnabled = true;
                btnRebuild.IsEnabled = false;
                ViewModel.Progress = 0;
                int count = 0;
                await Task.Run(() =>
                  {
                      count = ru.Rebuild(ViewModel.OutputDir,ViewModel.OverrideWhenExisted,out List<RebuildError> rebuildErrors);
                      ViewModel.RebuildErrors = rebuildErrors;
                  });
                    if (count == 0)
                    {
                        await CommonDialog.ShowOkDialogAsync("重建完成", $"没有任何文件被重建");
                    }
                    else
                    {
                        if (ViewModel.RebuildErrors.Count == 0)
                        {
                            await CommonDialog.ShowOkDialogAsync("重建成功", $"共{count}个文件");
                        }
                        btnRebuild.IsEnabled = true;
                    }
                }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                stkConfig.IsEnabled = true;
                btnStop.IsEnabled = false;
                ViewModel.Message = "就绪";
                ViewModel.Progress = ViewModel.ProgressMax;
            }
        }

        private void TreeViewItem_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if ((sender as TreeViewItem).DataContext is FreeFileSystemTree file)
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

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            btnStop.IsEnabled = false;
            ru.Stop();
        }
    }


    public class RebuildPanelViewModel : INotifyPropertyChanged
    {
        private FreeFileSystemTree fileTree;
        private string inputDir;

        private string message = "就绪";

        private string outputDir;

        private double progress;

        private double progressMax;

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
        public double Progress
        {
            get => progress;
            set => this.SetValueAndNotify(ref progress, value, nameof(Progress));
        }
        public double ProgressMax
        {
            get => progressMax;
            set => this.SetValueAndNotify(ref progressMax, value, nameof(ProgressMax));
        }
        private bool overrideWhenExisted;
        public bool OverrideWhenExisted
        {
            get => overrideWhenExisted;
            set => this.SetValueAndNotify(ref overrideWhenExisted, value, nameof(OverrideWhenExisted));
        }
        private List<RebuildError> rebuildErrors = new List<RebuildError>();
        public List<RebuildError> RebuildErrors
        {
            get => rebuildErrors;
            set => this.SetValueAndNotify(ref rebuildErrors, value, nameof(RebuildErrors));
        }

    }

}
