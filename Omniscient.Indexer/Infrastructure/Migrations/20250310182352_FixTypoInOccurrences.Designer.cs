﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using Omniscient.Indexer.Infrastructure;

#nullable disable

namespace Omniscient.Indexer.Infrastructure.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250310182352_FixTypoInOccurrences")]
    partial class FixTypoInOccurrences
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "9.0.2")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("Omniscient.Shared.Entities.Email", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("uuid");

                    b.Property<string>("Content")
                        .IsRequired()
                        .HasColumnType("text");

                    b.Property<string>("FileName")
                        .IsRequired()
                        .HasColumnType("text");

                    b.HasKey("Id");

                    b.ToTable("Emails");
                });

            modelBuilder.Entity("Omniscient.Shared.Entities.Occurence", b =>
                {
                    b.Property<string>("WordValue")
                        .HasColumnType("text");

                    b.Property<Guid>("EmailId")
                        .HasColumnType("uuid");

                    b.Property<int>("Count")
                        .HasColumnType("integer");

                    b.HasKey("WordValue", "EmailId");

                    b.HasIndex("EmailId");

                    b.ToTable("Occurrences");
                });

            modelBuilder.Entity("Omniscient.Shared.Entities.Word", b =>
                {
                    b.Property<string>("Value")
                        .HasColumnType("text");

                    b.HasKey("Value");

                    b.ToTable("Words");
                });

            modelBuilder.Entity("Omniscient.Shared.Entities.Occurence", b =>
                {
                    b.HasOne("Omniscient.Shared.Entities.Email", "Email")
                        .WithMany()
                        .HasForeignKey("EmailId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("Omniscient.Shared.Entities.Word", "Word")
                        .WithMany()
                        .HasForeignKey("WordValue")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Email");

                    b.Navigation("Word");
                });
#pragma warning restore 612, 618
        }
    }
}
