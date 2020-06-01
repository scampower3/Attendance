using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.IO;
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
using NLog;
using System.Text.RegularExpressions;
namespace Attendance
{
    /// <summary>
    /// Interaction logic for Upload.xaml
    /// </summary>
    public partial class Upload : Page
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();
        public Upload()
        {
            InitializeComponent();
        }

        private void Browse_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog fileDialog = new Microsoft.Win32.OpenFileDialog();
            fileDialog.Filter = "Image files (*.jpg, *.jpeg, *.jpe, *.jfif, *.png) | *.jpg; *.jpeg; *.jpe; *.jfif; *.png"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = fileDialog.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Open document
                string filename = fileDialog.FileName;
                Filepath.Text = filename;
            }
        }

        private void UploadFile_Click(object sender, RoutedEventArgs e)
        {
            var connectionstring = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
            using (SqlConnection connection = new SqlConnection(connectionstring))
            {
                string studentID = StudentID.Text;
                string name = StudentName.Text;
                string filepath = Filepath.Text;
                string studentIDpatten = "^[0-9]+$";

                if (studentID == "" || name == "" || filepath == "")
                {
                    MessageBox.Show("Please fill up all the textboxes");
                }
                else if (Regex.IsMatch(studentID, studentIDpatten) == false)
                {
                    MessageBox.Show("Please input only integer in the Student ID field");
                }
                else
                {
                    FileInfo file = new FileInfo(filepath);
                    byte[] blob = new byte[file.Length];
                    using (FileStream fs = file.OpenRead())
                    {
                        fs.Read(blob, 0, blob.Length);
                    }
                    connection.Open();
                    string query = "insert into Attendance.dbo.TrainingData ([StudentID],[FullName],[Face]) values(@StudentID,@FullName,@Face)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@StudentID", SqlDbType.Int).Value = studentID;
                        command.Parameters.Add("@FullName", SqlDbType.VarChar).Value = name;
                        command.Parameters.Add("@Face", SqlDbType.VarBinary).Value = blob;
                        try
                        {
                            command.ExecuteNonQuery();
                            MessageBox.Show("Upload Successful");
                            StudentID.Text = "";
                            StudentName.Text = "";
                            Filepath.Text = "";
                        }
                        catch (Exception ex)
                        {
                            logger.Debug(ex);
                        }
                    }
                }
            }
        }

        private void Back_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Choice());
        }
    }
}
