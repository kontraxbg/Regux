namespace Kontrax.Regux.Model
{
    public static class Role
    {
        public const string GlobalAdmin = "GlobalAdmin";
        public const string GlobalAdminName = "Централен администратор";
    }

    public static class AccessLevel
    {
        public const string Admin = "Admin";
        public const string Manager = "Manager";
        public const string Employee = "Employee";
    }

    public static class LocalRoleName
    {
        public const string None = "(без ограничение)";
    }

    public static class ThisSystem
    {
        public const string Name = "RegUX";
        public const string Oid = "2.16.100.1.1.43.1.1";
    }
}
