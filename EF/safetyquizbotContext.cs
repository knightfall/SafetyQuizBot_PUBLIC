using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using SafetyQuizBot.Models;

namespace SafetyQuizBot.EF
{
    public partial class safetyquizbotContext : DbContext
    {
        public safetyquizbotContext()
        {
        }

        public safetyquizbotContext(DbContextOptions<safetyquizbotContext> options)
            : base(options)
        {
        }

        public virtual DbSet<FeedBack> FeedBack { get; set; }
        public virtual DbSet<QuizState> QuizState { get; set; }
        public virtual DbSet<ScoreBoard> ScoreBoard { get; set; }
        public virtual DbSet<ProfileInfo> UserTable { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseMySQL("server=SERVER_IP_HERE;port=3306;user=safetyquizbot;password=INSERT_PASSWORD_HERE;database=safetyquizbot");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "2.2.3-servicing-35854");

            modelBuilder.Entity<FeedBack>(entity =>
            {
                entity.ToTable("FeedBack", "safetyquizbot");

                entity.Property(e => e.FeedbackId)
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Comment)
                    .HasMaxLength(150)
                    .IsUnicode(false);

                entity.Property(e => e.UserId)
                    .HasMaxLength(45)
                    .IsUnicode(false);
            });

            modelBuilder.Entity<QuizState>(entity =>
            {
                entity.HasKey(e => e.Uid);

                entity.ToTable("quizState", "safetyquizbot");

                entity.Property(e => e.Uid)
                    .HasColumnName("UID")
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.CurrentContext)
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.LastState).HasColumnType("int(11)");
            });

            modelBuilder.Entity<ScoreBoard>(entity =>
            {
                entity.HasKey(e => e.SessionId);

                entity.ToTable("scoreBoard", "safetyquizbot");

                entity.Property(e => e.SessionId)
                    .HasColumnName("SessionID")
                    .HasMaxLength(50)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.CorrectInput).HasColumnType("int(11)");

                entity.Property(e => e.CorrectTap).HasColumnType("int(11)");

                entity.Property(e => e.IncorrectInput).HasColumnType("int(11)");

                entity.Property(e => e.IncorrectTap).HasColumnType("int(11)");

                entity.Property(e => e.Input).HasColumnType("int(11)");

                entity.Property(e => e.PartialInput).HasColumnType("int(11)");

                entity.Property(e => e.Psid)
                    .IsRequired()
                    .HasColumnName("PSID")
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.State)
                    .HasMaxLength(45)
                    .IsUnicode(false);

                entity.Property(e => e.Tap).HasColumnType("int(11)");

                entity.Property(e => e.Timestamp).HasColumnType("int(20)");
            });

            modelBuilder.Entity<ProfileInfo>(entity =>
            {
                entity.ToTable("UserTable", "safetyquizbot");

                entity.Property(e => e.Id)
                    .HasColumnName("ID")
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .ValueGeneratedNever();

                entity.Property(e => e.Email)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.FirstName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.LastName)
                    .HasMaxLength(100)
                    .IsUnicode(false);

                entity.Property(e => e.ProfilePic)
                    .HasMaxLength(300)
                    .IsUnicode(false);

                entity.Property(e => e.Verified)
                    .HasMaxLength(10)
                    .IsUnicode(false);
            });
        }
    }
}
