﻿// <auto-generated />
using Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.EntityFrameworkCore.Storage.Internal;
using System;

namespace Database.Migrations
{
    [DbContext(typeof(Context))]
    [Migration("20180429003902_PlayerStrength")]
    partial class PlayerStrength
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "2.0.2-rtm-10011")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", 0/*SqlServerValueGenerationStrategy.IdentityColumn*/);

            modelBuilder.Entity("Database.Game", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("BlackPlayerID");

                    b.Property<int>("BlackPlayerStrength");

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<int>("CurrentPlayer");

                    b.Property<int>("CurrentPosition");

                    b.Property<string>("Fen");

                    b.Property<int>("GameStatus");

                    b.Property<string>("InitialPosition");

                    b.Property<byte[]>("RowVersion")
                        .IsConcurrencyToken();

                    b.Property<int>("Variant");

                    b.Property<Guid>("WhitePlayerID");

                    b.Property<int>("WhitePlayerStrength");

                    b.HasKey("ID");

                    b.ToTable("Games");
                });

            modelBuilder.Entity("Database.PdnMove", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("CreatedOn")
                        .ValueGeneratedOnAddOrUpdate();

                    b.Property<string>("DisplayString");

                    b.Property<bool?>("IsJump");

                    b.Property<string>("Move");

                    b.Property<int?>("PieceTypeMoved");

                    b.Property<int?>("Player");

                    b.Property<string>("ResultingFen");

                    b.Property<Guid>("TurnID");

                    b.HasKey("ID");

                    b.HasIndex("TurnID");

                    b.ToTable("Moves");
                });

            modelBuilder.Entity("Database.PdnTurn", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<Guid>("GameID");

                    b.Property<int>("MoveNumber");

                    b.HasKey("ID");

                    b.HasIndex("GameID");

                    b.ToTable("Turns");
                });

            modelBuilder.Entity("Database.Player", b =>
                {
                    b.Property<Guid>("ID")
                        .ValueGeneratedOnAdd();

                    b.Property<string>("ConnectionID");

                    b.HasKey("ID");

                    b.ToTable("Players");
                });

            modelBuilder.Entity("Database.PdnMove", b =>
                {
                    b.HasOne("Database.PdnTurn", "Turn")
                        .WithMany("Moves")
                        .HasForeignKey("TurnID")
                        .OnDelete(DeleteBehavior.Cascade);
                });

            modelBuilder.Entity("Database.PdnTurn", b =>
                {
                    b.HasOne("Database.Game", "Game")
                        .WithMany("Turns")
                        .HasForeignKey("GameID")
                        .OnDelete(DeleteBehavior.Cascade);
                });
#pragma warning restore 612, 618
        }
    }
}
