using System;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;
using System.Xml.Linq;
using Nijo.CodeGenerating;

namespace Nijo.Ui.Views {
    /// <summary>
    /// アプリケーション全体設定画面
    /// </summary>
    public partial class AppConfigView : UserControl {
        private readonly CodeRenderingConfig _config;
        private readonly DataGridView _gridView;
        private readonly XDocument _document;

        public AppConfigView(CodeRenderingConfig config) {
            _config = config;

            // XMLドキュメントのインスタンスを取得するためのプライベートフィールドを取得
            var fieldInfo = typeof(CodeRenderingConfig).GetField("_xDocument", BindingFlags.NonPublic | BindingFlags.Instance);
            _document = (XDocument)fieldInfo.GetValue(_config);

            InitializeComponent();

            // DataGridViewの作成と設定
            _gridView = new DataGridView {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                MultiSelect = false
            };

            // 列の設定
            _gridView.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = "設定項目",
                Name = "PropertyName",
                Width = 240,
                ReadOnly = true,
            });
            _gridView.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = "値",
                Name = "Value"
            });
            _gridView.Columns.Add(new DataGridViewTextBoxColumn {
                HeaderText = "説明",
                Name = "Description",
                ReadOnly = true,
                Width = 640,
            });

            // セル値変更イベントを設定
            _gridView.CellValueChanged += GridView_CellValueChanged;

            Controls.Add(_gridView);

            // 設定項目をリフレクションで取得して表示
            LoadConfigProperties();
        }

        private void InitializeComponent() {
            this.SuspendLayout();
            //
            // AppConfigView
            //
            this.Name = "AppConfigView";
            this.Size = new System.Drawing.Size(800, 600);
            this.ResumeLayout(false);
        }

        /// <summary>
        /// CodeRenderingConfigのプロパティをリフレクションで取得してDataGridViewに表示
        /// </summary>
        private void LoadConfigProperties() {
            Type configType = typeof(CodeRenderingConfig);
            PropertyInfo[] properties = configType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            foreach (PropertyInfo property in properties) {
                // Description属性を取得
                var descriptionAttribute = property.GetCustomAttribute<DescriptionAttribute>();
                string description = descriptionAttribute?.Description ?? string.Empty;

                // プロパティの値を取得
                object value = property.GetValue(_config);

                // DataGridViewに行を追加
                _gridView.Rows.Add(property.Name, value?.ToString() ?? "", description);
            }
        }

        /// <summary>
        /// セル値が変更されたときのイベントハンドラ
        /// </summary>
        private void GridView_CellValueChanged(object? sender, DataGridViewCellEventArgs e) {
            // 値の列でない場合は何もしない
            if (e.ColumnIndex != 1 || e.RowIndex < 0) return;

            // 変更された行からプロパティ名を取得
            string propertyName = _gridView.Rows[e.RowIndex].Cells[0].Value.ToString();
            string newValue = _gridView.Rows[e.RowIndex].Cells[1].Value.ToString();

            // XMLのルート要素にアトリビュートとして設定を保存
            if (_document.Root != null) {
                _document.Root.SetAttributeValue(propertyName, newValue);

                try {
                    // XMLを保存
                    _document.Save(_document.BaseUri);
                    MessageBox.Show($"{propertyName}の値を更新しました。", "設定の更新", MessageBoxButtons.OK, MessageBoxIcon.Information);
                } catch (Exception ex) {
                    MessageBox.Show($"設定の保存中にエラーが発生しました：{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
