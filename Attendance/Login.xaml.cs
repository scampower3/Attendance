using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
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
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Page
    {
        public Login()
        {
            InitializeComponent();
        }
        private void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService.Navigate(new Signup());
        }

        private void Signin_Click(object sender, RoutedEventArgs e)
        {
            string password = Password.Password;
            string username = Username.Text;

            if (password != "" && username != "")
            {
                var connectionstring = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    connection.Open();
                    string query = "SELECT * FROM Attendance.dbo.USERS";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            bool match= false;
                            while (reader.Read())
                            {
                                string salt = reader.GetString(2);
                                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100000);
                                string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                                if (hash == reader.GetString(1) && username == reader.GetString(0))
                                {
                                    match = true;
                                    this.NavigationService.Navigate(new FaceRecognition());
                                }
                            }
                            if(match == false)
                            {
                                MessageBox.Show("Account not found");
                            }
                        }
                    }
                }
            }
            else if (password == "" || username == "")
            {
                MessageBox.Show("Username/Password Cannot be empty");
            }
            else
            {
                MessageBox.Show("Does not match criteria");
            }
        }
        static string RandomString(int length)
        {
            const string valid = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ1234567890";
            StringBuilder res = new StringBuilder();
            using (RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider())
            {
                byte[] uintBuffer = new byte[sizeof(uint)];

                while (length-- > 0)
                {
                    rng.GetBytes(uintBuffer);
                    uint num = BitConverter.ToUInt32(uintBuffer, 0);
                    res.Append(valid[(int)(num % (uint)valid.Length)]);
                }
            }

            return res.ToString();

        }

    }
}
