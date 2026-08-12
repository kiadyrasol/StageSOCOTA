using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Projet> Projets { get; set; }
        public DbSet<RFC> RFCs { get; set; }
        public DbSet<ActionProjet> Actions { get; set; }
        public DbSet<Commentaire> Commentaires { get; set; }
        public DbSet<PieceJointe> PiecesJointes { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RFC>()
                .HasOne(r => r.Champion)
                .WithMany()
                .HasForeignKey(r => r.ChampionId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RFC>()
                .HasOne(r => r.Sponsor)
                .WithMany()
                .HasForeignKey(r => r.SponsorId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ActionProjet>()
                .HasOne(a => a.Responsable)
                .WithMany()
                .HasForeignKey(a => a.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Commentaire>()
                .HasOne(c => c.Auteur)
                .WithMany()
                .HasForeignKey(c => c.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}