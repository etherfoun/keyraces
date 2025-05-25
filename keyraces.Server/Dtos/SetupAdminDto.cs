namespace keyraces.Server.Dtos
{
    public class SetupAdminDto
    {
        public string AdminEmail { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
    }
}
