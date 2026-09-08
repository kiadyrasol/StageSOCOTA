using Microsoft.EntityFrameworkCore;
using GestionProjetSocota.Models;

namespace GestionProjetSocota.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Utilisateur> Utilisateurs { get; set; }
        public DbSet<Projet> Projets { get; set; }
        public DbSet<RFC> RFCs { get; set; }
        public DbSet<ActionProjet> Actions { get; set; }
        public DbSet<Commentaire> Commentaires { get; set; }
        public DbSet<PieceJointe> PiecesJointes { get; set; }
        public DbSet<HistoriqueProjet> HistoriqueProjets { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ReferenceCompteur> ReferenceCompteurs { get; set; }

        public DbSet<UniteProjet> UnitesProjets { get; set; }
        public DbSet<DepartementProjet> DepartementsProjets { get; set; }
        public DbSet<TypeProjetReference> TypesProjets { get; set; }
        public DbSet<PlateformeProjet> PlateformesProjets { get; set; }
        public DbSet<JournalErreur> JournauxErreurs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // =========================================================
            // RFC
            // =========================================================

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


            // =========================================================
            // ACTIONS
            // =========================================================

            modelBuilder.Entity<ActionProjet>()
                .HasOne(a => a.Responsable)
                .WithMany()
                .HasForeignKey(a => a.ResponsableId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // COMMENTAIRES
            // =========================================================

            modelBuilder.Entity<Commentaire>()
                .HasOne(c => c.Auteur)
                .WithMany()
                .HasForeignKey(c => c.AuteurId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // HISTORIQUE
            // =========================================================

            modelBuilder.Entity<HistoriqueProjet>()
                .HasOne(h => h.Utilisateur)
                .WithMany()
                .HasForeignKey(h => h.UtilisateurId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // PROJET → UNITÉ
            // =========================================================

            modelBuilder.Entity<Projet>()
                .HasOne(p => p.UniteProjet)
                .WithMany()
                .HasForeignKey(p => p.UniteProjetId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // PROJET → DÉPARTEMENT
            // =========================================================

            modelBuilder.Entity<Projet>()
                .HasOne(p => p.DepartementProjet)
                .WithMany()
                .HasForeignKey(p => p.DepartementProjetId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // PROJET → TYPE
            // =========================================================

            modelBuilder.Entity<Projet>()
                .HasOne(p => p.TypeProjetReference)
                .WithMany()
                .HasForeignKey(p => p.TypeProjetReferenceId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // PROJET → PLATEFORME
            // =========================================================

            modelBuilder.Entity<Projet>()
                .HasOne(p => p.PlateformeProjet)
                .WithMany()
                .HasForeignKey(p => p.PlateformeProjetId)
                .OnDelete(DeleteBehavior.Restrict);


            // =========================================================
            // REFERENCE COMPTEUR → UNITÉ
            // =========================================================

            modelBuilder.Entity<ReferenceCompteur>()
                .HasOne(r => r.UniteProjet)
                .WithMany()
                .HasForeignKey(r => r.UniteProjetId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ReferenceCompteur>()
                .HasIndex(r => r.UniteProjetId)
                .IsUnique();


            // =========================================================
            // INDEX UNIQUES DES CLASSIFICATIONS
            // =========================================================

            modelBuilder.Entity<UniteProjet>()
                .HasIndex(u => u.Nom)
                .IsUnique();

            modelBuilder.Entity<DepartementProjet>()
                .HasIndex(d => d.Nom)
                .IsUnique();

            modelBuilder.Entity<TypeProjetReference>()
                .HasIndex(t => t.Nom)
                .IsUnique();

            modelBuilder.Entity<PlateformeProjet>()
                .HasIndex(p => p.Nom)
                .IsUnique();


            // =========================================================
            // DONNÉES INITIALES : UNITÉS
            // =========================================================

            modelBuilder.Entity<UniteProjet>().HasData(
                new UniteProjet
                {
                    Id = 1,
                    Nom = "CTN",
                    Prefixe = "CF",
                    Actif = true
                },
                new UniteProjet
                {
                    Id = 2,
                    Nom = "SGL",
                    Prefixe = "SG",
                    Actif = true
                },
                new UniteProjet
                {
                    Id = 3,
                    Nom = "CRE",
                    Prefixe = "CR",
                    Actif = true
                }
            );


            // =========================================================
            // DONNÉES INITIALES : DÉPARTEMENTS
            // =========================================================

            modelBuilder.Entity<DepartementProjet>().HasData(
                new DepartementProjet
                {
                    Id = 1,
                    Nom = "CAL",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 2,
                    Nom = "IND",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 3,
                    Nom = "LOG",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 4,
                    Nom = "IT",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 5,
                    Nom = "PRO",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 6,
                    Nom = "MPF",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 7,
                    Nom = "QUA",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 8,
                    Nom = "SUST",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 9,
                    Nom = "CTE",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 10,
                    Nom = "SALES",
                    Actif = true
                },
                new DepartementProjet
                {
                    Id = 11,
                    Nom = "PLN",
                    Actif = true
                }
            );


            // =========================================================
            // DONNÉES INITIALES : TYPES DE PROJET
            // =========================================================

            modelBuilder.Entity<TypeProjetReference>().HasData(
                new TypeProjetReference
                {
                    Id = 1,
                    Nom = "InHouse",
                    Actif = true
                },
                new TypeProjetReference
                {
                    Id = 2,
                    Nom = "Outsourced",
                    Actif = true
                }
            );


            // =========================================================
            // DONNÉES INITIALES : PLATEFORMES
            // =========================================================

            modelBuilder.Entity<PlateformeProjet>().HasData(
                new PlateformeProjet
                {
                    Id = 1,
                    Nom = "WEB",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 2,
                    Nom = "GPAO",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 3,
                    Nom = "PBI",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 4,
                    Nom = "SUN",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 5,
                    Nom = "Oracle",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 6,
                    Nom = "CRP",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 7,
                    Nom = "Mobile",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 8,
                    Nom = "SEAM",
                    Actif = true
                },
                new PlateformeProjet
                {
                    Id = 9,
                    Nom = "FREvolve",
                    Actif = true
                }
            );
        }
    }
}