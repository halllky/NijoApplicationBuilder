using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace HalApplicationBuilder {
    /// <summary>
    /// dotnet ef のラッパー
    /// </summary>
    public partial class HalappProjectMigrator {

        internal HalappProjectMigrator(HalappProject project) {
            _project = project;
        }
        private readonly HalappProject _project;

        private bool _build = false;
        private void Build() {
            if (_build) return;
            _build = true;

            // このクラスの処理が走っているとき、基本的には dotnet run も並走しているので、Releaseビルドを指定しないとビルド先が競合して失敗してしまう
            using var process = _project.CreateProcess("dotnet", "build", "--configuration", "Release");
            process.Start();
        }

        /// <summary>
        /// データベースが存在しない場合に新規作成します。
        /// </summary>
        /// <returns></returns>
        public HalappProjectMigrator EnsureCreateDatabase() {

            // sqliteファイル出力先フォルダが無い場合は作成する
            var dbDir = Path.Combine(_project.ProjectRoot, "bin", "Debug");
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            if (!GetMigrations().Any()) {
                AddMigration();
            }
            Migrate();

            return this;
        }
        /// <summary>
        /// データベースおよび全てのマイグレーションを削除します。
        /// </summary>
        public HalappProjectMigrator DeleteDatabaseAndMigrations() {
            // マイグレーションの削除
            var migrationDir = Path.Combine(_project.ProjectRoot, "Migrations");
            if (Directory.Exists(migrationDir)) {
                foreach (var file in Directory.GetFiles(migrationDir)) {
                    File.Delete(file);
                }
            }
            // DBの削除
            File.Delete(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3"));
            File.Delete(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3-shm"));
            File.Delete(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3-wal"));

            return this;
        }

        internal IEnumerable<Migration> GetMigrations() {
            try {
                Build();

                using var process = _project.CreateProcess(
                    "dotnet", "ef", "migrations", "list",
                    "--prefix-output", // ビルド状況やの行頭には "info:" が、マイグレーション名の行頭には "data:" がつくので、その識別のため
                    "--configuration", "Release",
                    "--no-build");

                var regex = MigrationDataLineRegex();
                return process
                    .Read()
                    .Select(line => regex.Match(line))
                    .Where(match => match.Success)
                    .Select(match => new Migration {
                        Name = match.Groups[1].Value,
                        Pending = match.Groups.Count == 3,
                    })
                    .ToArray();
            } catch (Exception) {
                return Enumerable.Empty<Migration>();
            }
        }
        internal void RemoveMigrationsUntil(string migrationName) {
            Build();

            // そのマイグレーションが適用済みだと migrations remove できないので、まず database update する
            using var update = _project.CreateProcess(
                "dotnet", "ef", "database", "update", migrationName,
                "--configuration", "Release",
                "--no-build");
            update.Start();

            // リリース済みマイグレーションより後のマイグレーションを消す
            while (GetMigrations().Last().Name != migrationName) {
                using var remove = _project.CreateProcess(
                    "dotnet", "ef", "migrations", "remove",
                    "--configuration", "Release",
                    "--no-build");
                remove.Start();
            }
        }
        internal void AddMigration() {
            Build();

            var migrationCount = GetMigrations().Count();
            var nextMigrationId = migrationCount.ToString("000000000000");

            using var cmd = _project.CreateProcess(
                "dotnet", "ef", "migrations", "add", nextMigrationId,
                "--configuration", "Release",
                "--no-build");
            cmd.Start();

            // マイグレーションファイルが追加されたことにより再ビルドが必要
            _build = false;
        }
        internal void Migrate() {
            Build();
            using var update = _project.CreateProcess(
                "dotnet", "ef", "database", "update",
                "--configuration", "Release",
                "--no-build");
            update.Start();
        }

        internal struct Migration {
            internal string Name { get; set; }
            internal bool Pending { get; set; }
        }

        [GeneratedRegex(@"^data:\s*([^\s]+)(\s\(Pending\))?$", RegexOptions.Multiline)]
        private static partial Regex MigrationDataLineRegex();
    }
}
