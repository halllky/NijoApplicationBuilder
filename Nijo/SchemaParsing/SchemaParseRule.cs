using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.SchemaParsing;

/// <summary>
/// XMLスキーマ解釈ルール
/// </summary>
public class SchemaParseRule {
    /// <summary>
    /// ルート集約の種類
    /// </summary>
    public required IModel[] Models { get; init; }
    /// <summary>
    /// 値メンバーの種類
    /// </summary>
    public required IValueMemberType[] ValueMemberTypes { get; init; }
    /// <summary>
    /// オプション項目の種類
    /// </summary>
    public required NodeOption[] NodeOptions { get; init; }


    /// <summary>
    /// 既定の型解釈ルール
    /// </summary>
    public static SchemaParseRule Default() {
        var models = new IModel[] {
            new DataModel(),
            new QueryModel(),
            new CommandModel(),
            new StaticEnumModel(),
            new ValueObjectModel(),
        };
        var valueMemberTypes = new IValueMemberType[] {
            new ValueMemberTypes.Word(),
            new ValueMemberTypes.IntMember(),
            new ValueMemberTypes.DateTimeMember(),
            new ValueMemberTypes.DateMember(),
            new ValueMemberTypes.YearMonthMember(),
            new ValueMemberTypes.YearMember(),
            new ValueMemberTypes.Description(),
            new ValueMemberTypes.DecimalMember(),
            new ValueMemberTypes.BoolMember(),
            new ValueMemberTypes.ByteArrayMember(),
        };
        var nodeOptions = new NodeOption[] {
            BasicNodeOptions.DisplayName,
            BasicNodeOptions.DbName,
            BasicNodeOptions.LatinName,
            BasicNodeOptions.IsKey,
            BasicNodeOptions.IsRequired,
            BasicNodeOptions.GenerateDefaultQueryModel,
            BasicNodeOptions.GenerateBatchUpdateCommand,
            BasicNodeOptions.IsReadOnly,
            BasicNodeOptions.HasLifeCycle,
            BasicNodeOptions.MaxLength,
            BasicNodeOptions.CharacterType,
            BasicNodeOptions.TotalDigit,
            BasicNodeOptions.DecimalPlace,
            BasicNodeOptions.SequenceName,
        };
        return new() {
            Models = models,
            ValueMemberTypes = valueMemberTypes,
            NodeOptions = nodeOptions,
        };
    }

    /// <summary>
    /// このルールの整合性を検証します。
    /// </summary>
    /// <exception cref="InvalidOperationException">ルールに問題がある場合</exception>
    public void ThrowIfInvalid() {
        // スキーマ定義名重複チェック
        var appearedName = new HashSet<string>();
        var duplicates = new HashSet<string>();
        foreach (var name in Models.Select(m => m.SchemaName).Concat(ValueMemberTypes.Select(t => t.SchemaTypeName))) {
            if (appearedName.Contains(name)) {
                duplicates.Add(name);
            } else {
                appearedName.Add(name);
            }
        }
        if (duplicates.Count > 0) {
            throw new InvalidOperationException($"型名 {string.Join(", ", duplicates)} が重複しています。");
        }

        // オプション属性のキー重複チェック
        var groupedOptions = NodeOptions
            .GroupBy(opt => opt.AttributeName)
            .Where(group => group.Count() >= 2)
            .ToArray();
        if (groupedOptions.Length > 0) {
            throw new InvalidOperationException($"オプション属性名 {groupedOptions.Select(g => g.Key).Join(", ")} が重複しています。");
        }

        // 予約語
        if (NodeOptions.Any(opt => opt.AttributeName == SchemaParseContext.ATTR_NODE_TYPE)) {
            throw new InvalidOperationException($"{SchemaParseContext.ATTR_NODE_TYPE} という名前のオプション属性は定義できません。");
        }
    }

    /// <summary>
    /// nijo.xmlのスキーマ定義ルールを説明するためのドキュメントをmarkdown形式でレンダリングします。
    /// </summary>
    internal string RenderMarkdownDocument() {

        var codeRenderingConfigOptions = typeof(CodeRenderingConfig)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(prop => new {
                prop.Name,
                Description = prop.GetCustomAttribute<DescriptionAttribute>()?.Description ?? prop.Name,
            });

        return $$"""
            # NijoApplicationBuilder スキーマ定義仕様
            NijoApplicationBuilder は `nijo.xml` に記載されたデータ構造や型をもとにC#やTypeScriptのソースコードを自動生成します。
            ここでは、 `nijo.xml` に記載されるデータ構造等の記述方法を説明します。

            基本的な記述方法の例およびコード自動生成全体にかかるルールは以下です。

            ```xml
            <?xml version="1.0" encoding="utf-8" ?>

            <診察記録管理システム
            {{codeRenderingConfigOptions.SelectTextTemplate(opt => $$"""
              {{opt.Name}}="{{opt.Description.Replace("\"", "&quot;")}}"
            """)}}
            >
              <!-- 以下、自動生成されるオブジェクトごとにその構造を記述していきます。この部分の詳細なルールは次項に譲ります。ここから -->
              <顧客 Type="data-model" GenerateDefaultQueryModel="True" GenerateBatchUpdateCommand="True" LatinName="Master Data 01">
                <顧客ID Type="word" IsKey="True" />
                <顧客名 Type="word" />
                <生年月日 Type="datetime" />
                <年齢 Type="int" />
                <住所 Type="child">
                  <都道府県 Type="word"/>
                  <市町村 Type="word"/>
                  <番地以降 Type="word"/>
                </住所>
                <備考 Type="description" />
              </顧客>
              <予約 Type="data-model" LatinName="Transaction Data 01">
                <予約ID Type="word" IsKey="True" />
                <予約日時 Type="datetime" />
                <ひとこと Type="word" />
                <顧客 Type="ref-to:顧客"/>
                <予約区分 Type="予約区分"/>
                <予約メモ Type="description" />
              </予約>
              <診察 Type="data-model" LatinName="Transaction Data 02">
                <予約 Type="ref-to:予約" IsKey="True" />
                <診察開始時刻 Type="datetime" />
                <診察終了時刻 Type="datetime" />
                <体温 Type="int" />
                <血圧上 Type="int" />
                <血圧下 Type="int" />
                <メモ Type="description" />
                <処方 Type="children">
                  <薬剤ID Type="word" IsKey="True" />
                  <薬剤名 Type="word" />
                  <用量 Type="int" />
                  <用法 Type="word" />
                  <日数 Type="int" />
                  <備考 Type="description" />
                </処方>
              </診察>
              <予約区分 Type="enum">
                <初診 DisplayName="初診（しょしん）" key="1" />
                <再診 DisplayName="再診（さいしん）" key="2" />
              </予約区分>
              <!-- ここまで -->
            </診察記録管理システム>
            ```

            ## 用語定義
            - スキーマ定義: ルールに沿って記述されたnijo.xmlのことを指します。
            - ルート集約: xmlは要素を入れ子で階層表現することができますが、このうちxmlルート要素の直下に記述される要素を指します。

            ## モデル
            ルート集約は必ず1つの `{{SchemaParseContext.ATTR_NODE_TYPE}}` 属性を持ちます。
            ここで指定されたタイプは、そのルート集約からどのようなソースが生成されるかを表します。
            例えば {{nameof(Nijo.Models.DataModel)}} からは、そのデータのRDBMSでの定義を設定する処理や、登録更新処理に関するモジュールが生成され、
            {{nameof(Nijo.Models.QueryModel)}} からは、{{nameof(Nijo.Models.DataModel)}}のデータを人間や外部システムが閲覧するための検索や問い合わせに関するモジュールが生成されます。
            """;
    }
}
