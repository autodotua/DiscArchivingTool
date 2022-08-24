using System.Windows;
using System.Windows.Controls;
using Microsoft.WindowsAPICodePack.FzExtension;
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using DiscArchivingTool;
using static DiscArchivingTool.App;
using System.Collections;

namespace DiscArchivingTool
{
    /// <summary>
    /// PackingPanel.xaml 的交互逻辑
    /// </summary>
    public partial class PackingPanel : UserControl
    {
        PackingUtility fu = new PackingUtility();

        public PackingPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
            fu.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };
            fu.RebuildProgressUpdated += (s, e) =>
        {
            if (e.MaxValue != ViewModel.ProgressMax)
            {
                ViewModel.ProgressMax = e.MaxValue;
            }
            ViewModel.Progress = e.Value;
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
        private void BrowseOutputButton_Click(object sender, RoutedEventArgs e)
        {
            string path = new FileFilterCollection().CreateOpenFileDialog().GetFolderPath();
            if (path != null)
            {
                ViewModel.OutputDir = path;
            }
        }

        private void BtnSelect_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel.DiscFilePackages == null)
            {
                return;
            }
            switch ((sender as Button).Tag as string)
            {
                case "1":
                    foreach (var package in ViewModel.DiscFilePackages)
                    {
                        package.Checked = true;
                    }
                    break;
                case "2":
                    foreach (var package in ViewModel.DiscFilePackages)
                    {
                        package.Checked = false;
                    }
                    break;
                case "3":
                    foreach (var package in ViewModel.DiscFilePackages)
                    {
                        package.Checked = false;
                    }
                    foreach (DiscFilePackage package in lvwPackages.SelectedItems)
                    {
                        package.Checked = true;
                    }
                    break;
            }
        }

        private async void CheckButton_Click(object sender, RoutedEventArgs e)
        {
            if(string.IsNullOrWhiteSpace(ViewModel.Dir))
            {
                await CommonDialog.ShowErrorDialogAsync("源目录为空");
                return;
            }
            if(!Directory.Exists(ViewModel.Dir))
            {
                await CommonDialog.ShowErrorDialogAsync("源目录不存在");
                return;
            }
            if(ViewModel.DiscSize<100)
            {
                await CommonDialog.ShowErrorDialogAsync("单盘容量过小");
                return;
            }
            if(ViewModel.MaxDiscCount<1)
            {
                await CommonDialog.ShowErrorDialogAsync("盘片数量应大于等于1盘");
                return;
            }
            try
            {
                btnCheck.IsEnabled = false;
                await Task.Run(() =>
                {
                    ViewModel.Message = "正在查找文件";
                    fu.EnumerateAndOrderFiles(ViewModel.Dir, ViewModel.EarliestDateTime, ViewModel.BlackList, ViewModel.BlackListUseRegex);
                    ViewModel.Message = "正在处理文件";
                    fu.SplitToDiscs(ViewModel.DiscSize, ViewModel.MaxDiscCount);
                });
                var pkgs = fu.Packages.DiscFilePackages;
                if (fu.Packages.SizeOutOfRangeFiles.Count > 0)
                {
                    pkgs.Add(new DiscFilePackage()
                    {
                        Index = -1
                    });
                    pkgs[^1].Files.AddRange(fu.Packages.SizeOutOfRangeFiles);
                }
                ViewModel.DiscFilePackages = pkgs;
                ViewModel.Message = "就绪";
                grdBottom.IsEnabled = true;
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "查询文件失败");
            }
            finally
            {
                btnCheck.IsEnabled = true;
            }
        }
        private void CopyPackageTime(object sender, RoutedEventArgs e)
        {
            var tag = (sender as MenuItem).Tag as string;
            var package = (sender as MenuItem).DataContext as DiscFilePackage;
            Clipboard.SetText((tag == "1" ? package.EarliestTime : package.LatestTime).ToString(DateTimeFormat));
        }

        private async void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("导出目录为空");
                return;
            }
            if (!Directory.Exists(ViewModel.OutputDir))
            {
                await CommonDialog.ShowErrorDialogAsync("导出目录不存在");
                return;
            }
            if(!ViewModel.DiscFilePackages.Any(p=>p.Checked))
            {
                await CommonDialog.ShowErrorDialogAsync("没有任何被选中的文件包");
                return;
            }
            try
            {
                btnExport.IsEnabled = false;
                stkConfig.IsEnabled = false;
                btnStopExport.IsEnabled = true;
                ViewModel.Progress = 0;
                bool ok = true;
                if (Directory.EnumerateFileSystemEntries(ViewModel.OutputDir).Any())
                {
                    if (await CommonDialog.ShowYesNoDialogAsync("清空目录",
                        $"目录{ViewModel.OutputDir}不为空，{Environment.NewLine}导出前将清空部分目录。{Environment.NewLine}是否继续？"))
                    {
                        try
                        {
                            foreach (var index in fu.Packages.DiscFilePackages.Where(p => p.Checked).Select(p => p.Index))
                            {
                                FzLib.IO.WindowsFileSystem.DeleteFileOrFolder(Path.Combine(ViewModel.OutputDir, index.ToString()), true, true);
                            }
                        }
                        catch (Exception ex)
                        {
                            return;
                        }
                    }
                    else
                    {
                        ok = false;
                    }
                }
                if (ok)
                {
                    try
                    {

                        await Task.Run(async () =>
                        {
                            await fu.ExportAsync(ViewModel.OutputDir, ViewModel.PackingType, async msg =>
                             {
                                 int id = 0;
                                 await Dispatcher.Invoke(async () =>
                                  {
                                      id = await CommonDialog.ShowSelectItemDialogAsync(msg, new SelectDialogItem[]
                                    {
                                       new SelectDialogItem("重试"),
                                       new SelectDialogItem("跳过"),
                                       new SelectDialogItem("终止")
                                      });
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
                    catch (Exception ex)
                    {
                        await CommonDialog.ShowErrorDialogAsync(ex, "打包失败");
                        ViewModel.Message = "终止";
                    }
                }
            }

            finally
            {
                btnExport.IsEnabled = true;
                stkConfig.IsEnabled = true;
                btnStopExport.IsEnabled = false;
                ViewModel.Progress = ViewModel.ProgressMax;
            }
        }

        private void StopExportButton_Click(object sender, RoutedEventArgs e)
        {
            fu.Stop();
            btnStopExport.IsEnabled = false;
        }
    }


    public class PackingPanelViewModel : INotifyPropertyChanged
    {
        private string blackList = $"Thumbs.db{Environment.NewLine}desktop.ini";
        private string dir;
        private List<DiscFilePackage> discFilePackages;
        private int discSize = 4480;
        private DateTime earliestDateTime = new DateTime(1, 1, 1);
        private int maxDiscCount = 1000;
        private string message = "就绪";
        private string outputDir;
        private PackingType packingType = PackingType.Copy;
        private double progress;
        private double progressMax;
        private DiscFilePackage selectedPackage;

        public event PropertyChangedEventHandler PropertyChanged;
        public string BlackList
        {
            get => blackList;
            set => this.SetValueAndNotify(ref blackList, value, nameof(BlackList));
        }

        public bool BlackListUseRegex { get; set; } = false;
        public string Dir
        {
            get => dir;
            set => this.SetValueAndNotify(ref dir, value, nameof(Dir));
        }

        public List<DiscFilePackage> DiscFilePackages
        {
            get => discFilePackages;
            set => this.SetValueAndNotify(ref discFilePackages, value, nameof(DiscFilePackages));
        }

        public int DiscSize
        {
            get => discSize;
            set => this.SetValueAndNotify(ref discSize, value, nameof(DiscSize));
        }

        public int[] DiscSizes { get; } = new int[] { 700, 4480, 8500, 23500 };
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

        public string OutputDir
        {
            get => outputDir;
            set => this.SetValueAndNotify(ref outputDir, value, nameof(OutputDir));
        }

        public PackingType PackingType
        {
            get => packingType;
            set => this.SetValueAndNotify(ref packingType, value, nameof(PackingType));
        }

        public IEnumerable PackingTypes => Enum.GetValues(typeof(PackingType));
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

        public DiscFilePackage SelectedPackage
        {
            get => selectedPackage;
            set => this.SetValueAndNotify(ref selectedPackage, value, nameof(SelectedPackage));
        }
    }

}
