using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using haldoc.Schema;

namespace haldoc {

    [AggregateRoot]
    public class 設備 {
        [Key]
        public string 設備ID { get; set; }
        [Required, InstanceName]
        public string 設備名 { get; set; }

        public string 容量 { get; set; }
        [Children]
        public IList<_管理設定> 管理設定 { get; set; }

        public class _管理設定 {
            [Key]
            public 薬品 薬品 { get; set; }

            public string 濃度上限値 { get; set; }
        }
    }

    [AggregateRoot]
    public class 薬品 {
        [Key]
        public string 薬品ID { get; set; }
        [Required, InstanceName]
        public string 薬品名 { get; set; }

        public string 単位 { get; set; }
    }

    public class テストアプリケーション : ApplicationSchema {

        public static テストアプリケーション CreateWithDummyData() {
            var yakuhin = Enumerable
                .Range(0, 10)
                .Select(i => new 薬品 { 薬品ID = i.ToString(), 薬品名 = $"薬品{i}", 単位 = "cc" })
                .ToArray();
            var setsubi = Enumerable
                .Range(0, 10)
                .Select(i => new 設備 {
                    設備ID = i.ToString(),
                    設備名 = $"設備{i}",
                    容量 = $"{i * 100}リットル",
                    管理設定 = yakuhin.Take(2).Select(y => new 設備._管理設定 { 薬品 = y, 濃度上限値 = "999cc/L" }).ToList(),
                })
                .ToArray();

            var mockDB = yakuhin.Cast<object>().Union(setsubi).ToHashSet();
            return new(mockDB);
        }

        private テストアプリケーション(HashSet<object> db) : base(db) { }

        public override string ApplicationName => "サンプルシステム";

        protected override IEnumerable<Type> RegisterAggregates() {
            // root
            yield return typeof(設備);
            yield return typeof(薬品);

            // child
            yield return typeof(設備._管理設定);
        }
    }

    // デモンストレーション
    // - このファイルへのプロパティ追加で3画面全てに反映されることを確認
}
