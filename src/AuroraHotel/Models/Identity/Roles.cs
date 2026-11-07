namespace AuroraHotel.Models;

public static class Roles
{
    public const string Admin = "Admin", Receptionist = "Receptionist", Customer = "Customer";
    public const string Staff = Admin + "," + Receptionist;
}
