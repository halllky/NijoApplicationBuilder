using System;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using haldoc.Api;
using Microsoft.EntityFrameworkCore;

namespace haldoc {
    public partial class DynamicDbContext : DbContext {
        public DynamicDbContext() {
        }
        public DynamicDbContext(DbContextOptions<DynamicDbContext> options) : base(options) {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) {
            // optionsBuilder.UseSqlite("filename=:memory:");
        }

        //protected override void OnModelCreating(ModelBuilder modelBuilder) {
        //    var rootTypes = Assembly
        //        .GetExecutingAssembly()
        //        .GetTypes()
        //        .Where(type => type.GetCustomAttribute<AggregateRootAttribute>() != null);

        //    foreach (var type in rootTypes) {
        //        if (type != typeof(取引先)) continue;

        //        var entity = modelBuilder.Entity(type);
        //        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //        foreach (var prop in props) {
        //            if (prop.PropertyType == typeof(string)) {
        //                var propConfig = entity
        //                    .Property<string>(prop.Name);
        //            } else {
        //                entity.Ignore(prop.Name);
        //            }
        //        }

        //        // PK
        //        var keys = props.Where(prop => prop.GetCustomAttribute<KeyAttribute>() != null);
        //        entity.HasKey(keys.Select(key => key.Name).ToArray());

        //        // 集約定義には無いがDBにはあるカラムの定義方法
        //        //entity.Property<decimal>("てすと");
        //    }
        //}
    }
}
