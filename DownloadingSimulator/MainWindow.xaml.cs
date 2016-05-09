using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Threading;

namespace DownloadingSimulator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static double B = 1;
        static double KB = 1024 * B;
        static double MB = 1024 * KB;
        static double GB = 1024 * MB;
        static double TB = 1024 * GB;

        static Size[] sizes;
        static Speed[] speeds;

        Thread SimulationThread;

        public MainWindow()
        {
            InitializeComponent();

            sizes = new Size[]
            {
                new Size("КБ", KB),
                new Size("МБ", MB),
                new Size("ГБ", GB),
                new Size("ТБ", TB),
            };

            Sizes_comboBox.ItemsSource = sizes;
            Sizes_comboBox.SelectedIndex = 2;


            speeds = new Speed[]
            {
                new Speed(@"Кбит/c", KB / 8),
                new Speed(@"Мбит/c", MB / 8),
                new Speed(@"Гбит/c", GB / 8),                
            };

            Speeds_comboBox.ItemsSource = speeds;
            Speeds_comboBox.SelectedIndex = 1;
        }


        class Size
        {
            public string Abbreviation { get; set; }
            public double SizeInBytes { get; set; }

            public Size(string abbreviation, double sizeInBytes)
            {
                this.Abbreviation = abbreviation;
                this.SizeInBytes = sizeInBytes;
            }

            public override string ToString()
            {
                return this.Abbreviation;
            }
        }

        class Speed
        {
            public string Notation { get; set; }
            public double BytesPerSecond { get; set; }

            public Speed(string notation, double bytesPerSecond)
            {
                this.Notation = notation;
                this.BytesPerSecond = bytesPerSecond;
            }

            public override string ToString()
            {
                return this.Notation;
            }
        }

        private void Start_button_Click(object sender, RoutedEventArgs e)
        {
            if(SimulationThread != null)
            if (SimulationThread.IsAlive)
                SimulationThread.Abort();

            SimulationThread = new Thread(new ThreadStart(Simulation));
            SimulationThread.IsBackground = true;
            SimulationThread.Start();
        }
        

        void Simulation()
        {
            double filesize = 0, speed = 0;

            Dispatcher.Invoke(new Action(delegate
            {
                if (!double.TryParse(Filesize_textBox.Text.Replace('.', ','), out filesize)) return;
                if (!double.TryParse(Speed_textBox.Text.Replace('.', ','), out speed)) return;

                filesize *= ((Size)Sizes_comboBox.SelectedItem).SizeInBytes;
                speed *= ((Speed)Speeds_comboBox.SelectedItem).BytesPerSecond;

                progressBar.Maximum = filesize;
            }));

            var secondsToDownload = filesize / speed;
            var downloaded = 0.0;

            var dt = 100;
            var bytesPer_dt = speed * (dt / 1000.0);

            do
            {
                if (downloaded + bytesPer_dt > filesize)
                {
                    var leftBytes = filesize - downloaded;

                    downloaded = leftBytes;

                    var leftDt = (int)((leftBytes / speed) * 1000);

                    dt = leftDt;

                    Dispatcher.Invoke(new Action(delegate
                    {
                        progressBar.Value += leftBytes;
                    }));                    
                }
                else
                {
                    downloaded += bytesPer_dt;

                    Dispatcher.Invoke(new Action(delegate
                    {
                        progressBar.Value = downloaded;
                    }));
                }

                try
                {
                    var percentsDone = downloaded / (filesize / 100);
                    var left_Bytes = filesize - downloaded;
                    var timeLeft = TimeSpan.FromSeconds(left_Bytes / speed);

                    var labelText = $"{percentsDone:F1}% (осталось {TimeSpanToText(timeLeft, " ")})";

                    Dispatcher.Invoke(new Action(delegate
                    {
                        Statistics_label.Content = labelText;
                    }));
                }
                catch (Exception)
                {
                    MessageBox.Show($"Что-то пошло не так. Похоже, что процесс симулировать не удастся. Данное скачивание заняло бы {secondsToDownload} секунд (=");

                    Dispatcher.Invoke(new Action(delegate
                    {
                        progressBar.Value = 0;
                        Statistics_label.Content = "";
                    }));

                    SimulationThread.Abort();
                }

                Thread.Sleep(dt);
            } while (downloaded < filesize);

            MessageBox.Show($"Скачивание заняло:\n{TimeSpanToText(TimeSpan.FromSeconds(secondsToDownload), Environment.NewLine)}");

            Dispatcher.Invoke(new Action(delegate
            {
                progressBar.Value = 0;
                Statistics_label.Content = "";
            }));
        }


        static string TimeSpanToText(TimeSpan ts, string delimiter)
        {
            var text = "";

            if (ts.Days > 0)
                text += $"Дней: {ts.Days}" + delimiter;

            if (ts.Hours > 0)
                text += $"Часов: {ts.Hours}" + delimiter;

            if (ts.Minutes > 0)
                text += $"Минут: {ts.Minutes}" + delimiter;

            if (ts.Seconds > 0)
                text += $"Секунд: {ts.Seconds}" + delimiter;

            if (ts.Milliseconds > 0)
                text += $"Миллисекунд: {ts.Milliseconds}" + delimiter;

            return text;
        }
    }
}
