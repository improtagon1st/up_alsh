using System.Linq;
using System.Windows;
using up_melnilov_522.Helpers;

namespace up_melnilov_522.Windows
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UserSession.CurrentUser = null;
        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            UserSession.CurrentUser = null;

            var w = new ProductsWindow();
            w.Show();
            Close();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string login = TbLogin.Text.Trim();
            string pass = TbPassword.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(pass))
            {
                MessageBox.Show("Введите логин и пароль.");
                return;
            }

            using (var db = new Shoes_Melnikovv_522Entitiess())
            {
                var user = db.Users.FirstOrDefault(u => u.Username == login && u.Password == pass);

                if (user == null)
                {
                    MessageBox.Show("Неверный логин или пароль.");
                    return;
                }

              
                UserSession.CurrentUser = user;
            }

            var win = new ProductsWindow();
            win.Show();
            Close();
        }
    }
}
