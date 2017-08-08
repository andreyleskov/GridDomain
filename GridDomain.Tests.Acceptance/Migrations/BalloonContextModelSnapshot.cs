﻿using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using GridDomain.Tests.Acceptance.BalloonDomain;

namespace GridDomain.Tests.Acceptance.Migrations
{
    [DbContext(typeof(BalloonContext))]
    partial class BalloonContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasAnnotation("ProductVersion", "1.1.2")
                .HasAnnotation("SqlServer:ValueGenerationStrategy", SqlServerValueGenerationStrategy.IdentityColumn);

            modelBuilder.Entity("GridDomain.Tests.Acceptance.BalloonDomain.BalloonCatalogItem", b =>
                {
                    b.Property<Guid>("BalloonId")
                        .ValueGeneratedOnAdd();

                    b.Property<DateTime>("LastChanged");

                    b.Property<string>("Title");

                    b.Property<int>("TitleVersion");

                    b.HasKey("BalloonId");

                    b.ToTable("BalloonCatalog");
                });
        }
    }
}
