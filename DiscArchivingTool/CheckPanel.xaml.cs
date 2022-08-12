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

namespace DiscArchivingTool
{
    /// <summary>
    /// CheckPanel.xaml 的交互逻辑
    /// </summary>
    public partial class CheckPanel : UserControl
    {
        CheckUtility cu = new CheckUtility();

        public CheckPanel()
        {
            DataContext = ViewModel;
            InitializeComponent();
            cu.MessageReceived += (s, e) =>
            {
                ViewModel.Message = e.Message;
            };
            cu.CheckProgressUpdated += (s, e) =>
            {
                if (e.MaxValue != ViewModel.ProgressMax)
                {
                    ViewModel.ProgressMax = e.MaxValue;
                }
                ViewModel.Progress = e.Value;
            };
        }
        public CheckPanelViewModel ViewModel { get; } = new CheckPanelViewModel();



        private async void BtnCheck_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnCheck.IsEnabled = false;
                ViewModel.Message = "正在校验";
                await Task.Run(() =>
                {
                    cu.ReadFileList(ViewModel.Dir);
                    ViewModel.CheckResults = cu.Check();
                });
            }
            catch (Exception ex)
            {
                await CommonDialog.ShowErrorDialogAsync(ex, "校验失败");
            }
            finally
            {
                btnCheck.IsEnabled = true;
                ViewModel.Message = "就绪";
            }
        }


        private void BrowseDirButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FileFilterCollection().CreateOpenFileDialog();
            dialog.Multiselect = true;
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == Microsoft.WindowsAPICodePack.Dialogs.CommonFileDialogResult.Ok)
            {
                ViewModel.Dir = string.Join('|', dialog.FileNames);
            }
        }

        private void ChkErrorOnly_Checked(object sender, RoutedEventArgs e)
        {
            if (chkErrorOnly.IsChecked == true)
            {
                lvwResults.ItemsSource = ViewModel.ErrorOnlyCheckResults;
            }
            else
            {
                lvwResults.ItemsSource = ViewModel.CheckResults;
            }
        }
    }


    public class CheckPanelViewModel : INotifyPropertyChanged
    {
        private string dir;

        private string message = "就绪";


        private double progress;

        private double progressMax;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Dir
        {
            get => dir;
            set => this.SetValueAndNotify(ref dir, value, nameof(Dir));
        }
        public string Message
        {
            get => message;
            set => this.SetValueAndNotify(ref message, value, nameof(Message));
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
        private List<CheckResult> checkResults;
        public List<CheckResult> CheckResults
        {
            get => checkResults;
            set => this.SetValueAndNotify(ref checkResults, value, nameof(CheckResults),nameof(ErrorOnlyCheckResults));
        }

        public List<CheckResult> ErrorOnlyCheckResults => CheckResults.Where(p => p.NoProblem == false).ToList();
    }

}
