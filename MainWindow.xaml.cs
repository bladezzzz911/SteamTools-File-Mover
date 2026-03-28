using System;
using System.IO;
using System.IO.Compression;
using System.Windows;
using System.Windows.Media;
using System.Collections.ObjectModel;
using Microsoft.Win32;

namespace SteamFileMover
{
    public partial class MainWindow : Window
    {
        public ObservableCollection<LogMessage> LogEntries { get; set; } = new ObservableCollection<LogMessage>();

        public MainWindow()
        {
            InitializeComponent();
            LogList.ItemsSource = LogEntries;

            if (File.Exists("config.ini"))
            {
                if (int.TryParse(File.ReadAllText("config.ini"), out int idx))
                    ThemeBox.SelectedIndex = idx;
            }
            else { ThemeBox.SelectedIndex = 0; }

            AddToLog("Система готова", Brushes.Gray);
        }

        public class LogMessage
        {
            public required string Message { get; set; }
            public required Brush Color { get; set; }
        }

        private void AddToLog(string text, Brush? color = null)
        {
            Brush finalColor = color ?? (ThemeBox.SelectedIndex == 1 ? Brushes.Black : Brushes.White);
            if (ColorLogCb?.IsChecked == false) finalColor = ThemeBox.SelectedIndex == 1 ? Brushes.Black : Brushes.White;
            LogEntries.Insert(0, new LogMessage { Message = $"{DateTime.Now:HH:mm:ss} | {text}", Color = finalColor });
        }

        private void ProcessArchives_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog archiveDlg = new OpenFileDialog { Multiselect = true, Filter = "ZIP Archives (*.zip)|*.zip" };

            if (archiveDlg.ShowDialog() == true)
            {
                var folderDlg = new OpenFolderDialog { Title = "Выберите папку Steam" };

                if (folderDlg.ShowDialog() == true)
                {
                    string targetRoot = folderDlg.FolderName;
                    int count = 0;

                    foreach (string file in archiveDlg.FileNames)
                    {
                        try
                        {
                            using (var zip = ZipFile.OpenRead(file))
                            {
                                AddToLog($"📦 {Path.GetFileName(file)}", Brushes.Cyan);
                                foreach (var entry in zip.Entries)
                                {
                                    if (string.IsNullOrEmpty(entry.Name) || entry.FullName.EndsWith("/") || entry.FullName.EndsWith("\\")) continue;

                                    string ext = Path.GetExtension(entry.Name).ToLower();
                                    string sub = ext switch { ".manifest" => "depotcache", ".lua" or ".st" => "stplug-in", ".bin" => "StatsExport", _ => "" };

                                    if (!string.IsNullOrEmpty(sub))
                                    {
                                        string destDir = Path.Combine(targetRoot, "config", sub);
                                        if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                                        entry.ExtractToFile(Path.Combine(destDir, entry.Name), true);
                                        AddToLog($"  -> {entry.Name}", Brushes.LightGreen);
                                        count++;
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                    StatusLabel.Text = $"ГОТОВО: {count}";
                }
            }
        }

        private void ThemeBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (!this.IsInitialized) return;
            if (ThemeBox.SelectedIndex == 1)
            {
                Resources["WindowBg"] = Brushes.White;
                Resources["PanelBg"] = new SolidColorBrush(Color.FromRgb(240, 240, 240));
                Resources["TextMain"] = Brushes.Black;
                Resources["ControlBg"] = Brushes.White;
                Resources["ControlBorder"] = Brushes.LightGray;
            }
            else
            {
                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(15, 15, 15));
                Resources["PanelBg"] = new SolidColorBrush(Color.FromRgb(22, 22, 22));
                Resources["TextMain"] = Brushes.White;
                Resources["ControlBg"] = new SolidColorBrush(Color.FromRgb(37, 37, 38));
                Resources["ControlBorder"] = new SolidColorBrush(Color.FromRgb(51, 51, 51));
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            File.WriteAllText("config.ini", ThemeBox.SelectedIndex.ToString());
            MessageBox.Show("Тема сохранена!");
        }
    }
}