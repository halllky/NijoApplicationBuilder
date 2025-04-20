using Nijo.CodeGenerating;
using Nijo.ImmutableSchema;
using Nijo.Models;
using Nijo.Models.QueryModelModules;
using Nijo.SchemaParsing;
using Nijo.Util.DotnetEx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.ValueMemberTypes;

/// <summary>
/// バイト配列型
/// </summary>
internal class ByteArrayMember : IValueMemberType {
    public string TypePhysicalName => "ByteArray";
    public string SchemaTypeName => "bytearray";
    public string CsDomainTypeName => "byte[]";
    public string CsPrimitiveTypeName => "byte[]";
    public string TsTypeName => "/* バイト配列型のメンバーはクライアント側ソースにレンダリングされることはない想定 */";
    public UiConstraint.E_Type UiConstraintType => UiConstraint.E_Type.MemberConstraintBase;

    void IValueMemberType.Validate(XElement element, SchemaParseContext context, Action<XElement, string> addError) {
        // バイト配列型はDataModelにしか定義できない
        var isNotDataModel = context.TryGetModel(element, out var model) && model is not DataModel;

        var root = element.AncestorsAndSelf().Reverse().Skip(1).FirstOrDefault();
        var generateDefaultQueryModel = root != null && context.GetOptions(root).Contains(BasicNodeOptions.GenerateDefaultQueryModel);

        if (isNotDataModel) {
            addError(element, $"バイト配列を画面や帳票で用いることはできないため、{SchemaTypeName}は{nameof(DataModel)}にしか定義できません。");

        } else if (generateDefaultQueryModel) {
            addError(element, $"バイト配列を画面や帳票で用いることはできないため、{BasicNodeOptions.GenerateDefaultQueryModel.AttributeName}が指定された集約に{SchemaTypeName}を使用することはできません。");
        }
    }

    ValueMemberSearchBehavior? IValueMemberType.SearchBehavior => null; // バイト配列に対する検索は提供しない

    void IValueMemberType.RegisterDependencies(IMultiAggregateSourceFileManager ctx) {
        // 特になし
    }

    string IValueMemberType.RenderCreateDummyDataValueBody(CodeRenderingContext ctx) {
        return $$"""
            // ダミーデータとして8バイトのランダムなバイト配列を生成
            var dummyBytes = new byte[8];
            if (member.IsKey) {
                // キーの場合はシーケンス値を含む一意なデータを生成
                var seq = context.GetNextSequence();
                BitConverter.GetBytes(seq).CopyTo(dummyBytes, 0);
                context.Random.NextBytes(new Span<byte>(dummyBytes, 4, 4));
            } else {
                context.Random.NextBytes(dummyBytes);
            }
            return dummyBytes;
            """;
    }

    void IValueMemberType.RenderStaticSources(CodeRenderingContext ctx) {
        // 特になし
    }
}
