using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using DiscArchivingTool;

namespace DiscArchivingTool
{
    /// <summary>
    /// PackingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class PackingPanel : UserControl
    {
        FileUtility fu = new FileUtility();

        public PackingPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
            fu.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };
        }
        public PackingPanelViewModel ViewModel { get; } = new PackingPanelViewModel();
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.Dir = path;
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            await Task.Run(() =>
            {
                ViewModel.Message = "正在查找文件";
                fu.EnumerateAndOrderFiles(ViewModel.Dir, ViewModel.EarliestDateTime);
                ViewModel.Message = "正在查找文件";
                fu.SplitToDiscs(ViewModel.DiscSize, ViewModel.MaxDiscCount);
            });
            ViewModel.DiscFilePackages = fu.Packages;
            ViewModel.Message = "就绪";
            btnExport.IsEnabled = true;
        }
        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
                if (path != null)
                {
                    bool ok = true;
                    if (Directory.EnumerateFileSystemEntries(path).Any())
                    {
                        if (await CommonDialog.ShowYesNoDialogAsync("清空目录",
                            $"目录{path}不为空，{Environment.NewLine}导出前将清空目录。{Environment.NewLine}是否继续？"))
                        {
                            FzLib.IO.WindowsFileSystem.DeleteFileOrFolder(path, true, true);
                        }
                        else
                        {
                            ok = false;
                        }
                    }
                    if (ok)
                    {
                        btnExport.IsEnabled = false;
                        stkConfig.IsEnabled = false;
                        btnStopExport.IsEnabled = true;
                        try
                        {

                            await Task.Run(async () =>
                            {
                                await fu.ExportAsync(path, async msg =>
                                {
                                    var id = await CommonDialog.ShowSelectItemDialogAsync(msg, new SelectDialogItem[]
                                    {
                                       new SelectDialogItem("重试"),
                                       new SelectDialogItem("跳过"),
                                       new SelectDialogItem("终止")
                                    });
                                    return (ErrorOperation)id;
                                });
                            });
                            ViewModel.Message = "就绪";
                        }
                        catch (OperationCanceledException)
                        {
                            ViewModel.Message = "终止";
                        }
                    }
                }
            }
            finally
            {
                btnExport.IsEnabled = true;
                stkConfig.IsEnabled = true;
                btnStopExport.IsEnabled = false;
            }
        }

        private void StopExportButton_Click(object sender, RoutedEventArgs e)
        {
            fu.StopExporting();
            btnStopExport.IsEnabled = false;
        }

        private void CopyPackageTime(object sender, RoutedEventArgs e)
        {
            var tag = (sender as MenuItem).Tag as string;
            var package = (sender as MenuItem).DataContext as DiscFilePackage;
            Clipboard.SetText((tag == "1" ? package.EarliestTime : package.LatestTime).ToString(FileUtility.DateTimeFormat));
        }
    }


    public class PackingPanelViewModel : INotifyPropertyChanged
    {
        private string dir = @"O:\旧事重提\出行\个人-宁波\20220522-天一广场等";
        private DiscFilePackageCollection discFilePackages;

        private int discSize = 4480;

        private DateTime earliestDateTime = new DateTime(2000, 1, 1);

        private int maxDiscCount = 1000;

        private string message;

        private DiscFilePackage selectedPackage;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Dir
        {
            get => dir;
            set => this.SetValueAndNotify(ref dir, value, nameof(Dir));
        }
        public DiscFilePackageCollection DiscFilePackages
        {
            get => discFilePackages;
            set => this.SetValueAndNotify(ref discFilePackages, value, nameof(DiscFilePackages));
        }

        public int DiscSize
        {
            get => discSize;
            set => this.SetValueAndNotify(ref discSize, value, nameof(DiscSize));
        }

        public int[] DiscSizes { get; } = new int[] { 700, 4480, 8500, 22000 };

        public DateTime EarliestDateTime
        {
            get => earliestDateTime;
            set => this.SetValueAndNotify(ref earliestDateTime, value, nameof(EarliestDateTime));
        }
        public int MaxDiscCount
        {
            get => maxDiscCount;
            set => this.SetValueAndNotify(ref maxDiscCount, value, nameof(MaxDiscCount));
        }
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
        }

        public DiscFilePackage SelectedPackage
        {
            get => selectedPackage;
            set => this.SetValueAndNotify(ref selectedPackage, value, nameof(SelectedPackage));
        }
    }

}
