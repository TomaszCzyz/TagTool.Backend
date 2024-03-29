﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TagTool.Backend.DbContext;

#nullable disable

namespace TagTool.Backend.Migrations
{
    [DbContext(typeof(TagToolDbContext))]
    [Migration("20230711101056_Initial1")]
    partial class Initial1
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.4");

            modelBuilder.Entity("TagBaseTaggableItem", b =>
                {
                    b.Property<Guid>("TaggedItemsId")
                        .HasColumnType("TEXT");

                    b.Property<int>("TagsId")
                        .HasColumnType("INTEGER");

                    b.HasKey("TaggedItemsId", "TagsId");

                    b.HasIndex("TagsId");

                    b.ToTable("TagBaseTaggableItem");
                });

            modelBuilder.Entity("TagTool.Backend.Models.AssociationDescription", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("AssociationType")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagAssociationsId")
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TagAssociationsId");

                    b.HasIndex("TagId");

                    b.ToTable("AssociationDescriptions");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagAssociations", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("TagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("TagId");

                    b.ToTable("Associations");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagSynonymsGroup", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<string>("SynonymGroupName")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable("TagSynonymsGroup");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TaggableItem", b =>
                {
                    b.Property<Guid>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("TEXT");

                    b.HasKey("Id");

                    b.ToTable((string)null);

                    b.UseTpcMappingStrategy();
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.TagBase", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<DateTime?>("Added")
                        .HasColumnType("TEXT");

                    b.Property<DateTime?>("Deleted")
                        .HasColumnType("TEXT");

                    b.Property<string>("Discriminator")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("FormattedName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .UseCollation("NOCASE");

                    b.Property<DateTime?>("Modified")
                        .HasColumnType("TEXT");

                    b.Property<int?>("TagSynonymsGroupId")
                        .HasColumnType("INTEGER");

                    b.Property<int?>("TagsHierarchyId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("FormattedName")
                        .IsUnique();

                    b.HasIndex("TagSynonymsGroupId");

                    b.HasIndex("TagsHierarchyId");

                    b.ToTable("Tags");

                    b.HasDiscriminator<string>("Discriminator").HasValue("TagBase");

                    b.UseTphMappingStrategy();
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagsHierarchy", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("BaseTagId")
                        .HasColumnType("INTEGER");

                    b.HasKey("Id");

                    b.HasIndex("BaseTagId");

                    b.ToTable("TagsHierarchy");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TaggableFile", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.TaggableItem");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasIndex("Path")
                        .IsUnique();

                    b.ToTable("TaggableFiles");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TaggableFolder", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.TaggableItem");

                    b.Property<string>("Path")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasIndex("Path")
                        .IsUnique();

                    b.ToTable("TaggableFolders");
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.DayRangeTag", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.Tags.TagBase");

                    b.Property<int>("Begin")
                        .HasColumnType("INTEGER");

                    b.Property<int>("End")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("DayRangeTag");
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.DayTag", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.Tags.TagBase");

                    b.Property<int>("DayOfWeek")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("DayTag");

                    b.HasData(
                        new
                        {
                            Id = 1000,
                            FormattedName = "DayTag:Sunday",
                            DayOfWeek = 0
                        },
                        new
                        {
                            Id = 1001,
                            FormattedName = "DayTag:Monday",
                            DayOfWeek = 1
                        },
                        new
                        {
                            Id = 1002,
                            FormattedName = "DayTag:Tuesday",
                            DayOfWeek = 2
                        },
                        new
                        {
                            Id = 1003,
                            FormattedName = "DayTag:Wednesday",
                            DayOfWeek = 3
                        },
                        new
                        {
                            Id = 1004,
                            FormattedName = "DayTag:Thursday",
                            DayOfWeek = 4
                        },
                        new
                        {
                            Id = 1005,
                            FormattedName = "DayTag:Friday",
                            DayOfWeek = 5
                        },
                        new
                        {
                            Id = 1006,
                            FormattedName = "DayTag:Saturday",
                            DayOfWeek = 6
                        });
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.ItemTypeTag", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.Tags.TagBase");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.HasDiscriminator().HasValue("ItemTypeTag");

                    b.HasData(
                        new
                        {
                            Id = 3002,
                            FormattedName = "ItemTypeTag:TaggableFile",
                            Type = "TagTool.Backend.Models.TaggableFile, TagTool.Backend, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
                        },
                        new
                        {
                            Id = 3003,
                            FormattedName = "ItemTypeTag:TaggableFolder",
                            Type = "TagTool.Backend.Models.TaggableFolder, TagTool.Backend, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null"
                        });
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.MonthTag", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.Tags.TagBase");

                    b.Property<int>("Month")
                        .HasColumnType("INTEGER");

                    b.HasDiscriminator().HasValue("MonthTag");

                    b.HasData(
                        new
                        {
                            Id = 2001,
                            FormattedName = "MonthTag:January",
                            Month = 1
                        },
                        new
                        {
                            Id = 2002,
                            FormattedName = "MonthTag:February",
                            Month = 2
                        },
                        new
                        {
                            Id = 2003,
                            FormattedName = "MonthTag:March",
                            Month = 3
                        },
                        new
                        {
                            Id = 2004,
                            FormattedName = "MonthTag:April",
                            Month = 4
                        },
                        new
                        {
                            Id = 2005,
                            FormattedName = "MonthTag:May",
                            Month = 5
                        },
                        new
                        {
                            Id = 2006,
                            FormattedName = "MonthTag:June",
                            Month = 6
                        },
                        new
                        {
                            Id = 2007,
                            FormattedName = "MonthTag:July",
                            Month = 7
                        },
                        new
                        {
                            Id = 2008,
                            FormattedName = "MonthTag:August",
                            Month = 8
                        },
                        new
                        {
                            Id = 2009,
                            FormattedName = "MonthTag:September",
                            Month = 9
                        },
                        new
                        {
                            Id = 2010,
                            FormattedName = "MonthTag:October",
                            Month = 10
                        },
                        new
                        {
                            Id = 2011,
                            FormattedName = "MonthTag:November",
                            Month = 11
                        },
                        new
                        {
                            Id = 2012,
                            FormattedName = "MonthTag:December",
                            Month = 12
                        });
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.TextTag", b =>
                {
                    b.HasBaseType("TagTool.Backend.Models.Tags.TagBase");

                    b.Property<string>("Text")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .UseCollation("NOCASE");

                    b.HasDiscriminator().HasValue("TextTag");
                });

            modelBuilder.Entity("TagBaseTaggableItem", b =>
                {
                    b.HasOne("TagTool.Backend.Models.TaggableItem", null)
                        .WithMany()
                        .HasForeignKey("TaggedItemsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TagTool.Backend.Models.Tags.TagBase", null)
                        .WithMany()
                        .HasForeignKey("TagsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();
                });

            modelBuilder.Entity("TagTool.Backend.Models.AssociationDescription", b =>
                {
                    b.HasOne("TagTool.Backend.Models.TagAssociations", null)
                        .WithMany("Descriptions")
                        .HasForeignKey("TagAssociationsId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("TagTool.Backend.Models.Tags.TagBase", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagAssociations", b =>
                {
                    b.HasOne("TagTool.Backend.Models.Tags.TagBase", "Tag")
                        .WithMany()
                        .HasForeignKey("TagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("Tag");
                });

            modelBuilder.Entity("TagTool.Backend.Models.Tags.TagBase", b =>
                {
                    b.HasOne("TagTool.Backend.Models.TagSynonymsGroup", null)
                        .WithMany("TagsSynonyms")
                        .HasForeignKey("TagSynonymsGroupId");

                    b.HasOne("TagTool.Backend.Models.TagsHierarchy", null)
                        .WithMany("ChildTags")
                        .HasForeignKey("TagsHierarchyId");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagsHierarchy", b =>
                {
                    b.HasOne("TagTool.Backend.Models.Tags.TagBase", "BaseTag")
                        .WithMany()
                        .HasForeignKey("BaseTagId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("BaseTag");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagAssociations", b =>
                {
                    b.Navigation("Descriptions");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagSynonymsGroup", b =>
                {
                    b.Navigation("TagsSynonyms");
                });

            modelBuilder.Entity("TagTool.Backend.Models.TagsHierarchy", b =>
                {
                    b.Navigation("ChildTags");
                });
#pragma warning restore 612, 618
        }
    }
}
