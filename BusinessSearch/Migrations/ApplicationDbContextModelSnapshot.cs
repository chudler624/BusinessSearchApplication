﻿// <auto-generated />
using System;
using BusinessSearch.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BusinessSearch.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.14")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("BusinessSearch.Models.CrmEntry", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("BusinessName")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("BusinessStatus")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Company")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("datetime2");

                    b.Property<string>("Disposition")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Email")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Facebook")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FullAddress")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<double?>("GoogleRating")
                        .HasColumnType("float");

                    b.Property<string>("Industry")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Instagram")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Notes")
                        .HasMaxLength(1000)
                        .HasColumnType("nvarchar(1000)");

                    b.Property<string>("OpeningStatus")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Phone")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<string>("PhotoUrl")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<int?>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("Website")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("YelpUrl")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.HasKey("Id");

                    b.HasIndex("BusinessName");

                    b.HasIndex("DateAdded");

                    b.HasIndex("Disposition");

                    b.ToTable("CrmEntries");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmEntryList", b =>
                {
                    b.Property<int>("CrmEntryId")
                        .HasColumnType("int");

                    b.Property<int>("CrmListId")
                        .HasColumnType("int");

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("datetime2");

                    b.HasKey("CrmEntryId", "CrmListId");

                    b.HasIndex("CrmListId");

                    b.HasIndex("CrmEntryId", "CrmListId");

                    b.ToTable("CrmEntryList");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmList", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<int?>("AssignedToId")
                        .HasColumnType("int");

                    b.Property<DateTime>("CreatedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Description")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Industry")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<DateTime?>("LastModifiedDate")
                        .HasColumnType("datetime2");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("AssignedToId");

                    b.HasIndex("Industry");

                    b.HasIndex("Name");

                    b.ToTable("CrmLists");
                });

            modelBuilder.Entity("BusinessSearch.Models.SavedBusinessResult", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("BusinessId")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("BusinessStatus")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("City")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Country")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("Email")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Facebook")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("FullAddress")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Instagram")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<double>("Latitude")
                        .HasColumnType("float");

                    b.Property<double>("Longitude")
                        .HasColumnType("float");

                    b.Property<string>("Name")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("OpeningStatus")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("OwnerName")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("PhoneNumber")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<int>("PhotoCount")
                        .HasColumnType("int");

                    b.Property<string>("PhotoUrl")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("PlaceLink")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("PriceLevel")
                        .HasMaxLength(20)
                        .HasColumnType("nvarchar(20)");

                    b.Property<double>("Rating")
                        .HasColumnType("float");

                    b.Property<int>("ReviewCount")
                        .HasColumnType("int");

                    b.Property<string>("ReviewsLink")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<int>("SavedSearchId")
                        .HasColumnType("int");

                    b.Property<string>("State")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("StreetAddress")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("SubtypesJson")
                        .HasMaxLength(500)
                        .HasColumnType("nvarchar(500)");

                    b.Property<string>("Type")
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<bool>("Verified")
                        .HasColumnType("bit");

                    b.Property<string>("Website")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("YelpUrl")
                        .HasMaxLength(255)
                        .HasColumnType("nvarchar(255)");

                    b.Property<string>("Zipcode")
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("BusinessId");

                    b.HasIndex("Name");

                    b.HasIndex("SavedSearchId");

                    b.ToTable("SavedBusinessResults");
                });

            modelBuilder.Entity("BusinessSearch.Models.SavedSearch", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<string>("Industry")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<int>("ResultLimit")
                        .HasColumnType("int");

                    b.Property<DateTime>("SearchDate")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("datetime2")
                        .HasDefaultValueSql("GETUTCDATE()");

                    b.Property<int>("TotalResults")
                        .HasColumnType("int");

                    b.Property<string>("UserId")
                        .HasMaxLength(450)
                        .HasColumnType("nvarchar(450)");

                    b.Property<string>("ZipCode")
                        .IsRequired()
                        .HasMaxLength(10)
                        .HasColumnType("nvarchar(10)");

                    b.HasKey("Id");

                    b.HasIndex("Industry");

                    b.HasIndex("SearchDate");

                    b.HasIndex("UserId");

                    b.HasIndex("ZipCode");

                    b.ToTable("SavedSearches");
                });

            modelBuilder.Entity("BusinessSearch.Models.TeamMember", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("Id"));

                    b.Property<DateTime>("DateAdded")
                        .HasColumnType("datetime2");

                    b.Property<string>("Email")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.Property<string>("Role")
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)");

                    b.HasKey("Id");

                    b.HasIndex("Name");

                    b.HasIndex("Role");

                    b.ToTable("TeamMembers");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmEntryList", b =>
                {
                    b.HasOne("BusinessSearch.Models.CrmEntry", "CrmEntry")
                        .WithMany("CrmEntryLists")
                        .HasForeignKey("CrmEntryId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.HasOne("BusinessSearch.Models.CrmList", "CrmList")
                        .WithMany("CrmEntryLists")
                        .HasForeignKey("CrmListId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("CrmEntry");

                    b.Navigation("CrmList");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmList", b =>
                {
                    b.HasOne("BusinessSearch.Models.TeamMember", "AssignedTo")
                        .WithMany("AssignedLists")
                        .HasForeignKey("AssignedToId")
                        .OnDelete(DeleteBehavior.SetNull);

                    b.Navigation("AssignedTo");
                });

            modelBuilder.Entity("BusinessSearch.Models.SavedBusinessResult", b =>
                {
                    b.HasOne("BusinessSearch.Models.SavedSearch", "SavedSearch")
                        .WithMany("Results")
                        .HasForeignKey("SavedSearchId")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("SavedSearch");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmEntry", b =>
                {
                    b.Navigation("CrmEntryLists");
                });

            modelBuilder.Entity("BusinessSearch.Models.CrmList", b =>
                {
                    b.Navigation("CrmEntryLists");
                });

            modelBuilder.Entity("BusinessSearch.Models.SavedSearch", b =>
                {
                    b.Navigation("Results");
                });

            modelBuilder.Entity("BusinessSearch.Models.TeamMember", b =>
                {
                    b.Navigation("AssignedLists");
                });
#pragma warning restore 612, 618
        }
    }
}
