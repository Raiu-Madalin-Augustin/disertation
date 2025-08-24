namespace MiniShop.API.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public bool IsAdmin { get; set; } = false;
        public string Email { get; set; } = string.Empty;
        public Role Role { get; set; } = Role.Client;
        public ICollection<Order> Orders { get; set; }
        public ICollection<CartItem> CartItems { get; set; } = [];
    }
}
