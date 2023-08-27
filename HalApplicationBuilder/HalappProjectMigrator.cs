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

        internal HalappProjectMigrator(HalappProject project, TextWriter? log) {
            _project = project;
            _log = log;
        }
        private readonly HalappProject _project;

        private readonly TextWriter? _log;
        private void Logout(string message) {
            _log?.WriteLine($"{DateTime.Now}\t{message}");
        }

        private bool _skipBuild = false;

        /// <summary>
        /// データベースが存在しない場合に新規作成します。
        /// </summary>
        /// <returns></returns>
        public HalappProjectMigrator EnsureCreateDatabase() {
            var dbDir = Path.Combine(_project.ProjectRoot, "bin", "Debug");
            Logout($"EnsureCreateDatabase: {dbDir}");

            // sqliteファイル出力先フォルダが無い場合は作成する
            if (!Directory.Exists(dbDir)) Directory.CreateDirectory(dbDir);

            if (!GetMigrations().Any()) {
                AddMigration();
            }
            Migrate();

            return this;
        }

        /// <summary>
        /// データベースおよび全てのマイグレーションを削除し、作り直します。
        /// </summary>
        public HalappProjectMigrator DeleteAndRecreateDatabase() {
            void DeleteFile(string path) {
                Logout($"Delete: {path}");
                File.Delete(path);
            }

            // 既存DB,マイグレーションの削除
            var migrationDir = Path.Combine(_project.ProjectRoot, "Migrations");
            if (Directory.Exists(migrationDir)) {
                foreach (var file in Directory.GetFiles(migrationDir)) {
                    DeleteFile(file);
                }
            }
            DeleteFile(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3"));
            DeleteFile(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3-shm"));
            DeleteFile(Path.Combine(_project.ProjectRoot, "bin", "Debug", "debug.sqlite3-wal"));

            // マイグレーション,DB作成
            AddMigration("000000000000");
            Migrate();

            return this;
        }

        internal IEnumerable<Migration> GetMigrations() {
            try {
                using var process = _project.CreateProcess(
                    "dotnet", "ef", "migrations", "list",
                    "--prefix-output", // ビルド状況やの行頭には "info:" が、マイグレーション名の行頭には "data:" がつくので、その識別のため
                    "--configuration", "Release",
                    _skipBuild ? "--no-build" : "");

                var regex = MigrationDataLineRegex();
                var migrations = process
                    .Read()
                    .Select(line => regex.Match(line))
                    .Where(match => match.Success)
                    .Select(match => new Migration {
                        Name = match.Groups[1].Value,
                        Pending = match.Groups.Count == 3,
                    })
                    .ToArray();
                _skipBuild = true;
                return migrations;
            } catch (Exception) {
                return Enumerable.Empty<Migration>();
            }
        }
        internal void RemoveMigrationsUntil(string migrationName) {
            // そのマイグレーションが適用済みだと migrations remove できないので、まず database update する
            using var update = _project.CreateProcess(
                "dotnet", "ef", "database", "update", migrationName,
                "--configuration", "Release",
                _skipBuild ? "--no-build" : "");
            update.Start();
            _skipBuild = true;

            // リリース済みマイグレーションより後のマイグレーションを消す
            while (GetMigrations().Last().Name != migrationName) {
                using var remove = _project.CreateProcess(
                    "dotnet", "ef", "migrations", "remove",
                    "--configuration", "Release",
                    _skipBuild ? "--no-build" : "");
                remove.Start();
                _skipBuild = false;
            }
        }

        private string GenerateNextMigrationId() {
            var migrationCount = GetMigrations().Count();
            var nextMigrationId = migrationCount.ToString("000000000000");
            return nextMigrationId;
        }

        internal void AddMigration() {
            var nextMigrationId = GenerateNextMigrationId();
            AddMigration(nextMigrationId);
        }
        internal void AddMigration(string nextMigrationId) {
            using var cmd = _project.CreateProcess(
                "dotnet", "ef", "migrations", "add", nextMigrationId,
                "--configuration", "Release",
                _skipBuild ? "--no-build" : "");
            cmd.Start();
            _skipBuild = false;
        }

        internal void Migrate() {
            using var update = _project.CreateProcess(
                "dotnet", "ef", "database", "update",
                "--configuration", "Release",
                _skipBuild ? "--no-build" : "");
            update.Start();
            _skipBuild = true;
        }

        internal struct Migration {
            internal string Name { get; set; }
            internal bool Pending { get; set; }
        }

        [GeneratedRegex(@"^data:\s*([^\s]+)(\s\(Pending\))?$", RegexOptions.Multiline)]
        private static partial Regex MigrationDataLineRegex();
    }
}
