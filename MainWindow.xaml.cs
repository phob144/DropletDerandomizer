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
using osu.Game.Rulesets.Catch.Beatmaps;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Catch;
using System.Reflection;
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;

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

        private void derandomizationRateSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // update the textblock
            derandomizationRateTextBlock.Text = derandomizationRateSlider.Value.ToString();
        }

        private void derandomizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(beatmapPathTextBox.Text))
            {
                MessageBox.Show("Invalid file", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Beatmap derandomized = BeatmapOperator.DerandomizeDroplets(beatmapPathTextBox.Text, derandomizationRateSlider.Value);

            string filePath = $"{derandomized.MetadataSection.Artist} - {derandomized.MetadataSection.Title} ({derandomized.MetadataSection.Creator}) [{derandomized.MetadataSection.Version}].osu";

            // replace illegal characters with an empty string like osu does, taken from https://stackoverflow.com/questions/146134/how-to-remove-illegal-characters-from-path-and-filenames
            filePath = string.Join("", filePath.Split(Path.GetInvalidFileNameChars()));

            // specify the directory and write the beatmap
            derandomized.Write(
                Path.GetDirectoryName(beatmapPathTextBox.Text)
                + @"\"
                + filePath);

            MessageBox.Show("Done");
        }
    }
}
