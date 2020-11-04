using System;
using System.IO;
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
using Microsoft.Win32;
using DropletDerandomizer.Osu;
using osu.Game;
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch;
using System.Reflection;

namespace DropletDerandomizer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void browseButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                Filter = "Beatmap Files|*.osu",
                Multiselect = false
            };

            bool? result = fileDialog.ShowDialog();

            // null/false = error/action cancelled
            if (result != true)
                return;

            if (Path.GetExtension(fileDialog.FileName) != ".osu")
            {
                MessageBox.Show("Invalid file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            beatmapPathTextBox.Text = fileDialog.FileName;
        }

        private void derandomizeButton_Click(object sender, RoutedEventArgs e)
        {
            CatchBeatmap c = BeatmapOperator.Decode(beatmapPathTextBox.Text);

            MessageBox.Show(string.Join(", ", c.HitObjects[0].NestedHitObjects.Select(x => (x as CatchHitObject).X)));
        }
    }
}
