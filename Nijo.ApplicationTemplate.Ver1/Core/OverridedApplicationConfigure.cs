using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace MyApp.Core;

partial class OverridedApplicationConfigure {

    /// <summary>
    /// モデルのカスタマイズ
    /// </summary>
    public override void OnModelCreating(ModelBuilder modelBuilder) {
        // 診療収益分析SearchResult
        var 診療収益分析Entity = modelBuilder.Entity<診療収益分析SearchResult>();
        診療収益分析Entity.ToView("V_診療収益分析");
        診療収益分析Entity.HasKey(e => new { e.年月, e.診療科_診療科ID });

        // 機器分類別収益SearchResult
        var 機器分類別収益Entity = modelBuilder.Entity<機器分類別収益SearchResult>();
        機器分類別収益Entity.ToView("V_機器分類別収益");
        機器分類別収益Entity.HasKey(e => new { e.診療収益分析_年月, e.診療収益分析_診療科_診療科ID, e.機器分類_機器分類ID });

        // 機器別収益SearchResult
        var 機器別収益Entity = modelBuilder.Entity<機器別収益SearchResult>();
        機器別収益Entity.ToView("V_機器別収益");
        機器別収益Entity.HasKey(e => new { e.機器分類別収益_診療収益分析_年月, e.機器分類別収益_診療収益分析_診療科_診療科ID, e.機器分類別収益_機器分類_機器分類ID, e.医療機器_機器ID });

        // 時間帯別収益SearchResult
        var 時間帯別収益Entity = modelBuilder.Entity<時間帯別収益SearchResult>();
        時間帯別収益Entity.ToView("V_時間帯別収益");
        時間帯別収益Entity.HasKey(e => new { e.診療収益分析_年月, e.診療収益分析_診療科_診療科ID, e.時間帯 });

        // リレーションシップの設定
        診療収益分析Entity.HasMany(e => e.機器分類別収益)
            .WithOne(e => e.診療収益分析)
            .HasForeignKey(e => new {
                e.診療収益分析_年月,
                e.診療収益分析_診療科_診療科ID,
            });

        機器分類別収益Entity.HasMany(e => e.機器別収益)
            .WithOne(e => e.機器分類別収益)
            .HasForeignKey(e => new {
                e.機器分類別収益_診療収益分析_年月,
                e.機器分類別収益_診療収益分析_診療科_診療科ID,
                e.機器分類別収益_機器分類_機器分類ID,
            });

        診療収益分析Entity.HasMany(e => e.時間帯別収益)
            .WithOne(e => e.診療収益分析)
            .HasForeignKey(e => new {
                e.診療収益分析_年月,
                e.診療収益分析_診療科_診療科ID,
            });
    }
}
