using System.Security.Cryptography;
using System.Text;
using EventBookingSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace EventBookingSystem.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
            
        }

        public DbSet<Person> Persons { get; set; }
        public DbSet<Admin> Admins { get; set; }
        public DbSet<Organizer> Organizers { get; set; }
        public DbSet<Attendee> Attendees { get; set; }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventCategory> EventCategories { get; set; }
        public DbSet<Ticket> Tickets { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Add unique constraint for email
            modelBuilder.Entity<Person>()
                .HasIndex(p => p.Email)
                .IsUnique();

            // Seed default admin if not exists
            modelBuilder.Entity<Person>().HasData(new Person
            {
                PersonID = 1,
                FName = "admin",
                LName = "admin",
                Email = "admin@gmail.com",
                Password = "Admin@123",
                Role = "Admin"
            });

            modelBuilder.Entity<Admin>().HasData(new Admin
            {
                AdminID = 1,
                PersonID = 1
            });

            // Configure cascade delete for Admin
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.Person)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            // Configure cascade delete for Organizer
            modelBuilder.Entity<Organizer>()
                .HasOne(o => o.Person)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            // Configure cascade delete for Attendee
            modelBuilder.Entity<Attendee>()
                .HasOne(a => a.Person)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            // Configure cascade delete for Event
            modelBuilder.Entity<Event>()
                .HasOne(e => e.Organizer)
                .WithMany(o => o.Events)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Event>()
                .HasOne(e => e.Category)
                .WithMany(c => c.Events)
                .OnDelete(DeleteBehavior.Restrict);

            // Configure cascade delete for Ticket
            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Event)
                .WithMany()
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Ticket>()
                .HasOne(t => t.Attendee)
                .WithMany(a => a.Tickets)
                .OnDelete(DeleteBehavior.Restrict);

        }

        private string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }
    }
}