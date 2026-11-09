using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
namespace Insertion_Sort
{
    public sealed partial class MainPage : Page
    {
        /*______ГЛОБАЛЬНЫЕ_ПЕРЕМЕННЫЕ______*/
        private ObservableCollection<double> values = new ObservableCollection<double>();
        private int partIndex = 1;
        public MainPage()
        {
            this.InitializeComponent();
            instrumental.Translation += new Vector3(0, 0, 32);
            сlearButton.Translation = new Vector3(0, -30, 32);
            debugMode.Translation = new Vector3(180, 0, 32);
        }
        /*______ФУНКЦИИ_ПАНЕЛИ_ИНСТРУМЕНТОВ______*/
        private void insertionSort_Click(object sender, RoutedEventArgs e)
        {
            InsertionSort(values.ToArray());
        }
        private void addItem_Click(object sender, RoutedEventArgs e)
        {
            сlearButton.IsEnabled = true;
            insertionSort.IsEnabled = true;
            debugSort.IsEnabled = true;
            startText.Visibility = Visibility.Collapsed;
            if (textBox.Text != "")
            {
                AddTextboxItem(Regex.Split(textBox.Text, @"\s{1,}"));
            }
            else
            {
                Random random = new Random();
                values.Add(random.Next(-100, 101));
            }
        }
        private void AddTextboxItem(string[] itemArray)
        {
            string errorElement = "";
            foreach (var el in itemArray)
            {
                try
                {
                    values.Add(double.Parse(el));
                }
                catch
                {
                    errorElement += $"{el} ";
                }
            }
            if (errorElement != "")
            {
                infoBar.Message = errorElement;
                infoBar.IsOpen = true;
            }
            textBox.Text = "";
        }
        private void deleteItem_Click(object sender, RoutedEventArgs e)
        {
            if (values.Count > 0)
            {
                if (values.Count == 1)
                {
                    insertionSort.IsEnabled = false;
                    сlearButton.IsEnabled = false;
                    startText.Visibility = Visibility.Visible;
                }
                values.RemoveAt(values.Count - 1);
            }
        }
        /*______МАНИПУЛЯЦИИ_С_ЧИСЛАМИ______*/
        private void сlearButton_Click(object sender, RoutedEventArgs e)
        {
            insertionSort.IsEnabled = false;
            values.Clear();
            сlearButton.IsEnabled = false;
            debugSort.IsEnabled = false;
            startText.Visibility = Visibility.Visible;
        }
        private async void InsertionSort(double[] array)
        {
            ((Storyboard)instrumental.Resources["progressOpen"]).Begin();
            сlearButton.IsEnabled = false;
            for (int partIndex = 1; partIndex < array.Length; partIndex++)
            {
                double curUnsorted = array[partIndex];
                int i = 0;
                for (i = partIndex; i > 0 && array[i - 1] > curUnsorted; i--)
                {
                    array[i] = array[i - 1];
                }
                array[i] = curUnsorted;
                values.RemoveAt(partIndex);
                values.Insert(i, curUnsorted);
                await Task.Delay(1000);
            }
            ((Storyboard)instrumental.Resources["progressClose"]).Begin();
            сlearButton.IsEnabled = true;
        }
        private void сlearButton_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (сlearButton.IsEnabled)
            {
                сlearButton.Translation = new Vector3(0, 0, 32);
                сlearButton.Opacity = 1;
            }
            else
            {
                сlearButton.Translation = new Vector3(0, -30, 32);
                сlearButton.Opacity = 0;
            }
        }
        /*______ФУНКЦИИ_ОКНА_ОТЛАДКИ______*/
        private void debugSort_Click(object sender, RoutedEventArgs e)
        {
            сlearButton.IsEnabled = false;
            debugMode.Visibility = Visibility.Visible;
            debugMode.Translation = new Vector3(0, 0, 32);
            debugMode.Opacity = 1;
            instrumental.Translation = new Vector3(0, 50, 32);
            instrumental.Opacity = 0;
        }
        private void closeDebug_Click(object sender, RoutedEventArgs e)
        {
            сlearButton.IsEnabled = true;
            partIndex = 1;
            debugMode.Translation = new Vector3(180, 0, 32);
            debugMode.Opacity = 0;
            instrumental.Translation = new Vector3(0, 0, 32);
            instrumental.Opacity = 1;
            steps.Items.Clear();
            debugMode.Visibility = Visibility.Collapsed;
        }
        private async void nextStep_Click(object sender, RoutedEventArgs e)
        {
            if (partIndex < values.Count)
            {
                double[] array = values.ToArray();
                double curUnsorted = array[partIndex];
                int i = 0;
                for (i = partIndex; i > 0 && array[i - 1] > curUnsorted; i--)
                {
                    array[i] = array[i - 1];
                }
                array[i] = curUnsorted;
                values.RemoveAt(partIndex);
                values.Insert(i, curUnsorted);
                if (curUnsorted != array[partIndex])
                {
                    steps.Items.Add($"{curUnsorted} - перемещено");
                }
                else
                {
                    steps.Items.Add($"{curUnsorted} - осталось на месте");
                }
                await Task.Delay(10);
                partIndex += 1;
            }
        }
        private void data_Loaded(object sender, RoutedEventArgs e)
        {
            ((Border)sender).Translation = new Vector3(0, 0, 32);
        }
    }
}

