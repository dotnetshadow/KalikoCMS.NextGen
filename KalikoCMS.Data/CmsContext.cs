﻿namespace KalikoCMS.Data {
    using Entities;
    using Microsoft.EntityFrameworkCore;

    public class CmsContext : DbContext {
        internal DbSet<ContentAccessRightsEntity> ContentAccessRights { get; set; }
        internal DbSet<ContentEntity> Content { get; set; }
        internal DbSet<ContentLanguageEntity> ContentLanguages { get; set; }
        internal DbSet<ContentPropertyEntity> ContentProperties { get; set; }
        internal DbSet<ContentTypeEntity> ContentTypes { get; set; }
        internal DbSet<DomainEntity> Domains { get; set; }
        internal DbSet<LanguageEntity> Languages { get; set; }
        internal DbSet<PropertyEntity> Properties { get; set; }
        internal DbSet<PropertyTypeEntity> PropertyTypes { get; set; }
        internal DbSet<RedirectEntity> Redirects { get; set; }
        internal DbSet<SystemInformationEntity> SystemInformation { get; set; }
        internal DbSet<TagContextEntity> TagContexts { get; set; }
        internal DbSet<TagEntity> Tags { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<ContentTagEntity>()
                .ToTable("ContentTags")
                .HasKey(x => new {x.ContentId, x.TagId});

            // Remove constraint via Content since deleting Content will trigger Property which in turn triggers ContentProperty
            modelBuilder.Entity<ContentPropertyEntity>()
                .HasOne(x => x.Content)
                .WithMany(x => x.ContentProperties)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}