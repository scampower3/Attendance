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

namespace Attendance
{
    /// <summary>
    /// Interaction logic for Choice.xaml
    /// </summary>
    public partial class Choice : Page
    {
        public Choice()
        {
            InitializeComponent();
        }

        private void Face_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new FaceRecognition());
        }

        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Upload());
        }

        private void SignOut_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Login());
        }
    }
}
