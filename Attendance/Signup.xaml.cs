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
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;
using NLog;

namespace Attendance
{
    /// <summary>
    /// Interaction logic for Signup1.xaml
    /// </summary>
    public partial class Signup : Page
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        public Signup()
        {
            InitializeComponent();
        }

        private void CreateUser_Click(object sender, RoutedEventArgs e)
        {
            string password = Password.Text;
            string username = Username.Text;
            string MatchPasswordPattern = "^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8,}$";

            if (password != "" && username != "" && Regex.IsMatch(password, MatchPasswordPattern) == true)
            {
                bool success = true;
                string salt = RandomString(32);
                Rfc2898DeriveBytes pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100000);
                string hash = Convert.ToBase64String(pbkdf2.GetBytes(32));
                var connectionstring = ConfigurationManager.ConnectionStrings["Test"].ConnectionString;
                using (SqlConnection connection = new SqlConnection(connectionstring))
                {
                    connection.Open();
                    string query = "insert into Attendance.dbo.USERS ([username], [passwordhash], [salt]) values(@username,@passwordhash,@salt)";
                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.Add("@username", SqlDbType.VarChar).Value = username;
                        command.Parameters.Add("@passwordhash", SqlDbType.VarChar).Value = hash;
                        command.Parameters.Add("@salt", SqlDbType.VarChar).Value = salt;
                        try
                        {
                            command.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Username already exist");
                            logger.Debug(ex);
                            success = false;
                        }
                    }
                }
                if (success== true)
                    this.NavigationService.Navigate(new Login());
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
