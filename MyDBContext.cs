using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Xml;
using System.ComponentModel;
using Simbir_GO_Api.Helpers;

namespace Simbir_GO_Api
{
    public class MyDBContext : DbContext
    {
        public MyDBContext()
        {
            Database.EnsureCreated();

            var users = Users.ToList();
            var transports = Transports.ToList();
            var rentals = Rentals.ToList();

            if (users.Count == 0 && transports.Count == 0 && rentals.Count == 0)
            {
                CreateDefaultDataInTables();
            }
        }
        public MyDBContext(DbContextOptions<MyDBContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseNpgsql(Config.ConnectionString);
            }
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Transport> Transports { get; set; }
        public DbSet<Rental> Rentals { get; set; }
        public DbSet<BannedJWT> BannedJWTs { get; set; } 

        [Table("Users")]
        public class User
        {
            [Key]
            [Column("id")]
            public long Id { get; set; }

            [Required(ErrorMessage = "Имя пользователя обязательно")]
            [Column("username")]
            public string UserName { get; set; }

            [Required(ErrorMessage = "Пароль обязательно")]
            [Column("password")]
            public string Password { get; set; }

            [Column("is_admin")]
            public bool IsAdmin { get; set; }

            [Column("balance")]
            public double Balance { get; set; } 
        }

        [Table("Transports")]
        public class Transport  
        {
            [Key]
            [Column("id")]
            public long Id { get; set; }

            [Column("can_be_rented")]
            public bool CanBeRented { get; set; }

            [Column("transport_type")]
            [DefaultValue("")]
            public string TransportType { get; set; }

            [Column("model")]
            [DefaultValue("")]
            public string Model { get; set; }

            [Column("color")]
            [DefaultValue("")]
            public string Color { get; set; }

            [Column("identifier")]
            [DefaultValue("")]
            public string Identifier { get; set; }

            [Column("description")]
            [DefaultValue(null)]
            public string? Description { get; set; }

            [Column("latitude")]
            [DefaultValue(91)]
            public double Latitude { get; set; }

            [Column("longitude")]
            [DefaultValue(181)]
            public double Longitude { get; set; }

            [Column("minute_price")]
            [DefaultValue(null)]
            public double? MinutePrice { get; set; }

            [Column("day_price")]
            [DefaultValue(null)]
            public double? DayPrice { get; set; }

            [Column("owner_id")]
            [DefaultValue(-1)]
            public long OwnerId { get; set; }

            [ForeignKey("OwnerId")]
            public User User { get; set; }
        }

        [Table("Rentals")]
        public class Rental 
        {
            [Key]
            [Column("id")]
            public long Id { get; set; }

            [Column("user_id")]
            [DefaultValue(-1)]
            public long UserId { get; set; }

            [ForeignKey("UserId")]
            public User User { get; set; }

            [Column("transport_id")]
            [DefaultValue(-1)]
            public long TransportId { get; set; }

            [ForeignKey("TransportId")]
            public Transport Transport { get; set; }

            [Column("time_start")]
            [DefaultValue("")]
            public string TimeStart { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

            [Column("time_end")]
            [DefaultValue("")]
            public string? TimeEnd { get; set; }

            [Column("price_of_unit")]
            [DefaultValue(-1)]
            public double PriceOfUnit { get; set; }

            [Column("price_type")]
            public string PriceType { get; set; }

            [Column("final_price")]
            [DefaultValue(-1)]
            public double? FinalPrice { get; set; }
        }

        [Table("BannedJWTs")]
        public class BannedJWT
        {
            [Key]
            [Column("id")]
            public long Id { get; set; }

            [Column("token")]
            public string Token { get; set; }
        }

        private void CreateDefaultDataInTables() 
        {
            var newUser = new User { Id = 1, UserName = "admin", Password = HashForPass.HashPassword("123"), IsAdmin = true};
            Users.Add(newUser);

            newUser = new User { Id = 2, UserName = "user1", Password = HashForPass.HashPassword("123"), IsAdmin = false };
            Users.Add(newUser);

            var newTransport = new Transport { 
                Id = 1,
                CanBeRented = true,
                TransportType = "Car",
                Model = "Nissan",
                Color = "Red",
                Identifier = "123",
                Latitude = 50,
                Longitude = 40,
                MinutePrice = 30,
                DayPrice = 1000,
                OwnerId = 1
            };
            Transports.Add(newTransport);

            newTransport = new Transport
            {
                Id = 2,
                CanBeRented = false,
                TransportType = "Car",
                Model = "Lada",
                Color = "White",
                Identifier = "456",
                Latitude = 70,
                Longitude = 20,
                MinutePrice = 10,
                DayPrice = 600,
                OwnerId = 2
            };
            Transports.Add(newTransport);

            var newRental = new Rental { UserId = 1, TransportId = 2, PriceOfUnit = (double)newTransport.MinutePrice, PriceType = "Minutes"};
            Rentals.Add(newRental);

            SaveChanges();
        }
    }
}
