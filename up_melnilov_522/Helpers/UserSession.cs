namespace up_melnilov_522.Helpers
{
    public static class UserSession
    {
        public static Users CurrentUser { get; set; } // сущность из EDMX

        public static string FullName
        {
            get
            {
                if (CurrentUser == null) return "Гость";
                return CurrentUser.FullName;
            }
        }

        public static string Role
        {
            get
            {
                if (CurrentUser == null) return "Гость";
                return CurrentUser.Role; // "Администратор" / "Менеджер" / "Клиент"
            }
        }
    }
}
