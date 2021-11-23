using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace KeyBoardApp.Views
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        double orginalWidth, originalHeight;

        ScaleTransform scale = new ScaleTransform();

        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += MainWindow_MouserLeftButtonDown;
            this.Loaded += MainWindow_Loaded;
        }

        private void ChangeSize(double width, double height)
        {
            scale.ScaleX = width / orginalWidth;
            scale.ScaleY = height / originalHeight;

            FrameworkElement rootElement = this.Content as FrameworkElement;

            rootElement.LayoutTransform = scale;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            orginalWidth = this.Width;

            originalHeight = this.Height;

            if (this.WindowState == WindowState.Maximized)
                ChangeSize(this.ActualWidth, this.ActualHeight);

            this.SizeChanged += (s, evt) => 
            {
                ChangeSize(evt.NewSize.Width, evt.NewSize.Height);
            };

            var desktopWorkingArea = System.Windows.SystemParameters.WorkArea;
            this.Left = desktopWorkingArea.Right - this.Width;
            this.Top = desktopWorkingArea.Bottom - this.Height;

        }

        private void MainWindow_MouserLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
    }
}
