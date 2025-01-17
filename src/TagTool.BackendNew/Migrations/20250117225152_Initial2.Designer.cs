﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TagTool.BackendNew.DbContexts;

#nullable disable

namespace TagTool.BackendNew.Migrations
{
    [DbContext(typeof(TagToolDbContext))]
    [Migration("20250117225152_Initial2")]
    partial class Initial2
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.0");

            modelBuilder.Entity("TagTool.BackendNew.Entities.TagBase", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasMaxLength(60)
                        .HasColumnType("TEXT")
                        .UseCollation("NOCASE");

                    b.HasKey("Id");

                    b.HasIndex("Text")
                        .IsUnique();

                    b.ToTable("Tags");
                });

            modelBuilder.Entity("TagTool.BackendNew.Entities.TagBaseTaggableItem", b =>
                {
                    b.Property<int>("TagBaseId")
                        .HasColumnType("INTEGER");

                    b.Property<Guid>("TaggableItemId")
                        .HasColumnType("TEXT");

                    b.HasKey("TagBaseId", "TaggableItemId");

                    b.HasIndex("TaggableItemId");

                    b.ToTable("TagBaseTaggableItem");
                });

            modelBuilder.Entity("TagTool.BackendNew.Entities.TaggableItem", b =>
                {
                    b.Property<Guid>("Id")
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable((string)null);

                    b.UseTpcMappingStrategy();
                });

            modelBuilder.Entity("TagTool.BackendNew.TaggableFile.TaggableFile", b =>
                {
                    b.HasBaseType("TagTool.BackendNew.Entities.TaggableItem");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.ToTable("TaggableFile");
                });

            modelBuilder.Entity("TagTool.BackendNew.Entities.TagBaseTaggableItem", b =>
                {
                    b.HasOne("TagTool.BackendNew.Entities.TagBase", null)
                        .WithMany()
                        .HasForeignKey("TagBaseId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TagTool.BackendNew.Entities.TaggableItem", null)
                        .WithMany()
                        .HasForeignKey("TaggableItemId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });
#pragma warning restore 612, 618
        }
    }
}
