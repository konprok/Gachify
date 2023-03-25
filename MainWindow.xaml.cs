using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;
using OpenFileDialog = System.Windows.Forms.OpenFileDialog;
using SaveFileDialog = System.Windows.Forms.SaveFileDialog;
using File = System.IO.File;
using MessageBox = System.Windows.Forms.MessageBox;
using static System.Net.WebRequestMethods;
using System.Windows.Forms;

namespace projekt
{
    public partial class MainWindow : Window
    {
        private bool playlistDeleted = false;
        private SaveFileDialog saveFileDialog;
        private OpenFileDialog openFileDialog;
        bool wasBackButtonClicked = false;
        private string sPath;
        private List<int> unplayedIndexes = new List<int>();
        private List<int> playedIndexes = new List<int>();

        public MainWindow()
        {
            InitializeComponent();
            openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "Wybierz utowry";
            openFileDialog.DefaultExt = "mp3";
            openFileDialog.Filter = "Pliki muzyczne (*.mp3)|*.mp3|Pliki XML (*.xml)|*.xml|Wszystkie pliki (*.*)|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.Multiselect = true;
            saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Wybierz plik do zapisania";
            saveFileDialog.DefaultExt = "txt";
            saveFileDialog.Filter = openFileDialog.Filter;
            saveFileDialog.FilterIndex = 1;

            string lastFolder = LoadLastFolder();
            if (!string.IsNullOrEmpty(lastFolder))
            {
                sPath = lastFolder;
                if (Directory.Exists(lastFolder))
                {
                    var files = Directory.GetFiles(lastFolder, "*.mp3");
                    listBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f));
                }

                string relativePath = @"..\..\..\playlists";
                string folderPath = System.IO.Path.GetFullPath(relativePath);
                if (!Directory.Exists(folderPath))
                {
                    string whereisfolder = System.IO.Path.GetFullPath(@"..\..\..\..\projekt");
                    whereisfolder = whereisfolder + "\\";
                    Directory.CreateDirectory(whereisfolder + "playlists");
                }
                folderPath = folderPath + "\\";
                string[] fileNames = Directory.GetFiles(folderPath);
                playlist_listBox.ItemsSource = fileNames.Select(f => Path.GetFileNameWithoutExtension(f));
            }
            else
            {
                string relativePath = @"..\..\..\playlists";
                string folderPath = System.IO.Path.GetFullPath(relativePath);
                if (!Directory.Exists(folderPath))
                {
                    string whereisfolder = System.IO.Path.GetFullPath(@"..\..\..\..\projekt");
                    whereisfolder = whereisfolder + "\\";
                    Directory.CreateDirectory(whereisfolder + "playlists");
                }
            }

            mediaPlayer.MediaOpened += (sender, e) =>
            {
                progressBar.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;

            };

            mediaPlayer.MediaEnded += (sender, e) =>
            {
                progressBar.Value = 0;
            };

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        float volume = (float)mediaPlayer.Volume;
                        if (volume == 0)
                        {
                            volumeButton.Visibility = Visibility.Collapsed;
                            mutedButton.Visibility = Visibility.Visible;
                        }
                        else
                        {
                            volumeButton.Visibility = Visibility.Visible;
                            mutedButton.Visibility = Visibility.Collapsed;
                        }
                    });
                    Task.Delay(100).Wait();

                }
            });

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(() =>
                    {
                        progressBar.Value = mediaPlayer.Position.TotalSeconds;
                    });
                    Task.Delay(100).Wait();
                }
            });

            volumeSlider.SetBinding(Slider.ValueProperty, new System.Windows.Data.Binding("Volume") { Source = mediaPlayer });
            currentSongTextBlock.Text = "Aktualnie odtwarzany utwór: ";

        }

        private void SelectFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                var folder = dialog.SelectedPath;
                var files = Directory.GetFiles(folder, "*.mp3");
                sPath = folder;
                listBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f));
                MessageBox.Show("Znaleziono " + files.Length.ToString() + " plików MP3", "Wynik wyszukiwania");
                SaveLastFolder(folder);
            }
        }

        private TimeSpan currentPosition;

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {

            UpdatePlayPauseButtons();
            var selectedItem = listBox.SelectedItem as string;
            if (selectedItem != null)
            {
                string sFilePath = sPath + "/" + selectedItem + ".mp3";
                mediaPlayer.Open(new Uri(sFilePath));

                if (currentPosition == TimeSpan.Zero)
                {
                    mediaPlayer.Play();
                }
                else
                {
                    mediaPlayer.Position = currentPosition;
                    mediaPlayer.Play();
                }
            }
            else
            {
                NextSong();
            }

        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            UpdatePlayPauseButtons();
            currentPosition = mediaPlayer.Position;
            mediaPlayer.Pause();
        }

        private void ProgressBar_Click(object sender, MouseButtonEventArgs e)
        {
            var progressBar = sender as System.Windows.Controls.ProgressBar;
            var mousePosition = e.GetPosition(progressBar);
            var progress = mousePosition.X / progressBar.ActualWidth;
            mediaPlayer.Position = TimeSpan.FromSeconds(progressBar.Maximum * progress);
        }


        private void SkipButton_Click(object sender, RoutedEventArgs e)
        {
            NextSong();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (playedIndexes.Count > 1)
            {
                wasBackButtonClicked = true;
                playedIndexes.RemoveAt(playedIndexes.Count - 1);
                int previousIndex = playedIndexes[playedIndexes.Count - 1];
                listBox.SelectedIndex = previousIndex;
                PlaySelectedItem();
            }
        }
        private void PlaySelectedItem()
        {
            if (playButton.Visibility == Visibility.Visible)
            {
                UpdatePlayPauseButtons();
            }

            var selectedItem = listBox.SelectedItem as string;
            if (selectedItem != null)
            {
                string sFilePath = sPath + "/" + selectedItem + ".mp3";
                mediaPlayer.Open(new Uri(sFilePath));
                mediaPlayer.Play();
                if (!wasBackButtonClicked)
                {
                    if (loopButton.Visibility == Visibility.Collapsed)
                    {
                        playedIndexes.Add(listBox.SelectedIndex);
                    }
                }
                currentSongTextBlock.Text = "Aktualnie odtwarzany utwór: " + selectedItem;

            }

        }
        private void SaveLastFolder(string folderPath)
        {
            File.WriteAllText("lastFolder.txt", folderPath);
        }

        private string LoadLastFolder()
        {
            if (File.Exists("lastFolder.txt"))
            {
                return File.ReadAllText("lastFolder.txt");
            }
            return "";
        }

        private void MediaPlayer_MediaEnded(object sender, EventArgs e)
        {
            NextSong();
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            mediaPlayer.Close();
            mediaPlayer.Play();
            if (playButton.Visibility == Visibility.Collapsed)
            {
                UpdatePlayPauseButtons();
            }
            var selectedItem = listBox.SelectedItem as string;

            currentSongTextBlock.Text = "Aktualnie odtwarzany utwór: " + selectedItem;

            mediaPlayer.MediaEnded += (sender, e) =>
            {
                progressBar.Value = 0;
            };

        }

        private void UpdatePlayPauseButtons()
        {
            if (playButton.Visibility == Visibility.Collapsed)
            {
                pauseButton.Visibility = Visibility.Collapsed;
                playButton.Visibility = Visibility.Visible;

            }
            else
            {
                pauseButton.Visibility = Visibility.Visible;
                playButton.Visibility = Visibility.Collapsed;
            }
        }

        private void VolumeButton_Click(object sender, RoutedEventArgs e)
        {
            if (volumeSlider.Visibility == Visibility.Visible)
            {
                volumeSlider.Visibility = Visibility.Hidden;
            }
            else
            {
                volumeSlider.Visibility = Visibility.Visible;
            }
        }

        private void NextSong()
        {
            wasBackButtonClicked = false;
            if (shuffleButton.Visibility == Visibility.Visible)
            {
                if (unplayedIndexes.Count == 0)
                {
                    for (int i = 0; i < listBox.Items.Count; i++)
                    {
                        unplayedIndexes.Add(i);
                    }
                }

                int randomIndex = new Random().Next(0, unplayedIndexes.Count);
                int selectedIndex = unplayedIndexes[randomIndex];
                unplayedIndexes.RemoveAt(randomIndex);

                listBox.SelectedIndex = selectedIndex;
                PlaySelectedItem();
            }
            else if (alphabeticalButton.Visibility == Visibility.Visible)
            {
                unplayedIndexes.Clear();
                int selectedIndex = listBox.SelectedIndex;
                if (selectedIndex + 1 < listBox.Items.Count)
                {
                    listBox.SelectedIndex = selectedIndex + 1;
                    PlaySelectedItem();
                }
                else
                {
                    listBox.SelectedIndex = 0;
                    PlaySelectedItem();
                }

            }
            else if (loopButton.Visibility == Visibility.Visible)
            {
                PlaySelectedItem();
            }

        }

        private void ShuffleButton_Click(object sender, RoutedEventArgs e)
        {
            if (shuffleButton.Visibility == Visibility.Visible)
            {
                shuffleButton.Visibility = Visibility.Collapsed;
                loopButton.Visibility = Visibility.Visible;
            }
        }

        private void AlphabeticalButton_Click(object sender, RoutedEventArgs e)
        {
            if (alphabeticalButton.Visibility == Visibility.Visible)
            {
                shuffleButton.Visibility = Visibility.Visible;
                alphabeticalButton.Visibility = Visibility.Collapsed;
            }
        }
        private void LoopButton_Click(object sender, RoutedEventArgs e)
        {
            if (loopButton.Visibility == Visibility.Visible)
            {
                alphabeticalButton.Visibility = Visibility.Visible;
                loopButton.Visibility = Visibility.Collapsed;
            }
        }

        private void CreatePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            if (Directory.Exists(sPath))
            {
                createPlaylistItemsStackPanel.Visibility = Visibility.Visible;
                createPlaylistButton.Visibility = Visibility.Collapsed;
                cancelButton.Visibility = Visibility.Visible;
            }
            else
            { 
                MessageBox.Show("Choose your music folder first!");
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {

            string playlistName = playlistNameTextBox.Text;
            if (playlistName != "")
            {
                bool? wynik = openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                if (wynik.HasValue && wynik.Value)
                {
                    string[] fileNames = openFileDialog.FileNames;

                    string relativePath = @"..\..\..\playlists";
                    string folderPath = System.IO.Path.GetFullPath(relativePath);
                    folderPath = folderPath + "\\";
                    string filePath = Path.Combine(folderPath, playlistName);

                    using (StreamWriter sw = new StreamWriter(filePath))
                    {
                        foreach (string address in fileNames)
                        {
                            sw.WriteLine(address);
                        }
                    }
                    fileNames = Directory.GetFiles(folderPath);
                    playlist_listBox.ItemsSource = fileNames.Select(f => Path.GetFileNameWithoutExtension(f));
                    playlistNameTextBox.Text = "";
                }

                createPlaylistItemsStackPanel.Visibility = Visibility.Collapsed;
                createPlaylistButton.Visibility = Visibility.Visible;

            }
        }

        private void playlist_listBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (playlist_listBox.SelectedItem != null)
            {

                playedIndexes.Clear();
                mediaPlayer.Close();
                if (pauseButton.Visibility == Visibility.Visible)
                {
                    UpdatePlayPauseButtons();
                }
                string relativePath = @"..\..\..\playlists";
                string folderPath = System.IO.Path.GetFullPath(relativePath);
                folderPath = folderPath + "\\";
                var selectedItem = playlist_listBox.SelectedItem as string;
                string filePath = folderPath + selectedItem;

                string[] fileNames = File.ReadAllLines(filePath);
                listBox.ItemsSource = fileNames.Select(f => Path.GetFileNameWithoutExtension(f));
            }
        }

        private void allSongsButton_Click(object sender, RoutedEventArgs e)
        {

            playedIndexes.Clear();
            mediaPlayer.Close();
            if (pauseButton.Visibility == Visibility.Visible)
            {
                UpdatePlayPauseButtons();
            }
            string lastFolder = LoadLastFolder();
            sPath = lastFolder;
            var files = Directory.GetFiles(lastFolder, "*.mp3");
            listBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f));
            playButton.IsEnabled = true;
            playlist_listBox.SelectedItem = null;

        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            createPlaylistItemsStackPanel.Visibility = Visibility.Collapsed;
            createPlaylistButton.Visibility = Visibility.Visible;
            cancelButton.Visibility = Visibility.Collapsed;
        }

        private void DeletePlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string relativePath = @"..\..\..\playlists";
            string folderPath = System.IO.Path.GetFullPath(relativePath);
            folderPath = folderPath + "\\";
            var selectedItem = playlist_listBox.SelectedItem as string;
            string filePath = folderPath + selectedItem;
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }
            string[] fileNames = Directory.GetFiles(folderPath);
            playlist_listBox.ItemsSource = fileNames.Select(f => Path.GetFileNameWithoutExtension(f));
            string lastFolder = LoadLastFolder();
            var files = Directory.GetFiles(lastFolder, "*.mp3");
            listBox.ItemsSource = files.Select(f => Path.GetFileNameWithoutExtension(f));
            NextSong();
            mediaPlayer.Pause();
            UpdatePlayPauseButtons();
        }

        private void EditPlaylistButton_Click(object sender, RoutedEventArgs e)
        {
            string? playlistName = playlist_listBox.SelectedItem as string;
            if (playlistName != null)
            {
                bool? wynik = openFileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK;
                string[] playlistNames = openFileDialog.FileNames;

                string relativePath = @"..\..\..\playlists";
                string folderPath = System.IO.Path.GetFullPath(relativePath);
                folderPath = folderPath + "\\";
                string filePath = Path.Combine(folderPath, playlistName);

                using (StreamWriter sw = new StreamWriter(filePath))
                {
                    foreach (string address in playlistNames)
                    {
                        sw.WriteLine(address);
                    }
                }
                playlistNames = Directory.GetFiles(folderPath);
                playlist_listBox.ItemsSource = playlistNames.Select(f => Path.GetFileNameWithoutExtension(f));

                string[] fileNames = File.ReadAllLines(filePath);
                listBox.ItemsSource = fileNames.Select(f => Path.GetFileNameWithoutExtension(f));
            }
            NextSong();
            mediaPlayer.Pause();
            UpdatePlayPauseButtons();
        }
    }
}