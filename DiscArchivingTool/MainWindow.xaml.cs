using Microsoft.WindowsAPICodePack.FzExtension;
using System;
using System.Collections.Generic;
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
using FzLib;
using System.ComponentModel;
using System.IO;
using ModernWpf.FzExtension.CommonDialog;
using Mapster;
using FzLib.DataStorage.Serialization;

namespace DiscArchivingTool
{
    public partial class MainWindow : Window
    {
        Configs config = new Configs();
        string configPath = "configs.json";
        public MainWindow()
        {
            try
            {
                config.TryLoadFromJsonFile(configPath);
            }
            catch (Exception ex)
            {

            }
            InitializeComponent();

            config.Packing.Adapt(packing.ViewModel);
            config.Rebuild.Adapt(rebuild.ViewModel);
            config.Check.Adapt(checkout.ViewModel);
            config.Update.Adapt(update.ViewModel);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            packing.ViewModel.Adapt(config.Packing);
            rebuild.ViewModel.Adapt(config.Rebuild);
            checkout.ViewModel.Adapt(config.Check);
            update.ViewModel.Adapt(config.Update);
            config.Save(configPath);
        }
    }
}
