using Nijo.ImmutableSchema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.CodeGenerating.Helpers;

// ------------------------------------------
// メタデータの抽象度の世界（EFCoreEntityやDisplayDataはこのレベル）

/// <summary>
/// 自動生成されるソースコード上に表れる構造体のメタ情報。
/// 具体的には <see cref="IModel"/> の中でソースコードの生成に用いられるモジュールがこれに該当する。
/// </summary>
public interface IInstancePropertyOwnerMetadata {
    IEnumerable<IInstancePropertyMetadata> GetMembers();
}
/// <summary>
/// 自動生成されるソースコード上に表れるプロパティのうち、子孫をもたない値メンバーのメタ情報。
/// </summary>
public interface IInstanceValuePropertyMetadata : IInstancePropertyMetadata {
    IValueMemberType Type { get; }
}
/// <summary>
/// 自動生成されるソースコード上に表れるプロパティのうち、子孫をもつ構造体メンバーのメタ情報。単一のオブジェクトも配列も両方含む。
/// </summary>
public interface IInstanceStructurePropertyMetadata : IInstancePropertyMetadata, IInstancePropertyOwnerMetadata {
    /// <summary>配列の場合は List や [] 抜きのクラス名</summary>
    string CsType { get; }
    bool IsArray { get; }
}

/// <summary>
/// <see cref="IInstanceValuePropertyMetadata"/> or <see cref="IInstanceStructurePropertyMetadata"/>
/// </summary>
public interface IInstancePropertyMetadata {
    ISchemaPathNode SchemaPathNode { get; }
    string PropertyName { get; }
    string DisplayName => PropertyName; // TODO ver.1
}

// ------------------------------------------
// メタデータの抽象度の世界より1段階具体化された世界（自動生成されたあとのソースコードの抽象度の世界）: インタフェース

/// <summary>
/// 自動生成されるソースコード上に表れるインスタンスのプロパティ。
/// オーナーへの参照を持つ。
/// </summary>
public interface IInstanceProperty {
    /// <summary>このプロパティの親の親の一番大元の変数</summary>
    Variable Root { get; }
    /// <summary>このプロパティの親</summary>
    IInstancePropertyOwner Owner { get; }
    /// <inheritdoc cref="IInstancePropertyMetadata"/>
    IInstancePropertyMetadata Metadata { get; }
    bool IsNullable { get; }
}
/// <summary>
/// <see cref="IInstanceProperty"/> のオーナー
/// </summary>
public interface IInstancePropertyOwner {
    /// <summary>変数名 or プロパティ名</summary>
    string Name { get; }
    /// <inheritdoc cref="IInstancePropertyMetadata"/>
    IInstancePropertyOwnerMetadata Metadata { get; }
}

// ------------------------------------------
// メタデータの抽象度の世界より1段階具体化された世界（自動生成されたあとのソースコードの抽象度の世界）: 具象クラス

/// <summary>
/// 自動生成されるソースコードの中に表れる変数
/// </summary>
public sealed class Variable : IInstancePropertyOwner {
    /// <summary>
    /// 自動生成されるソースコードの中に表れる変数を表すインスタンスを作成します。
    /// </summary>
    /// <param name="name">変数名</param>
    /// <param name="metadata">変数の型</param>
    public Variable(string name, IInstancePropertyOwnerMetadata metadata) {
        Name = name;
        Metadata = metadata;
    }
    public string Name { get; }
    public IInstancePropertyOwnerMetadata Metadata { get; }
}
/// <summary>
/// 自動生成されるソースコードの中に表れる変数のプロパティのうち、子孫をもたない値メンバーのプロパティ。
/// </summary>
public sealed class InstanceValueProperty : IInstanceProperty {
    public required Variable Root { get; init; }
    public required IInstancePropertyOwner Owner { get; init; }
    public required IInstanceValuePropertyMetadata Metadata { get; init; }
    public bool IsNullable => true;

    IInstancePropertyMetadata IInstanceProperty.Metadata => Metadata;

    public override string ToString() {
        return $"{Metadata.PropertyName}(InstanceValueProperty)";
    }
}
/// <summary>
/// 自動生成されるソースコードの中に表れる変数のプロパティのうち、子孫をもつ構造体メンバーのプロパティ。
/// </summary>
public sealed class InstanceStructureProperty : IInstanceProperty, IInstancePropertyOwner {
    public required Variable Root { get; init; }
    public required IInstancePropertyOwner Owner { get; init; }
    public required IInstanceStructurePropertyMetadata Metadata { get; init; }
    public bool IsNullable => true;

    IInstancePropertyMetadata IInstanceProperty.Metadata => Metadata;
    IInstancePropertyOwnerMetadata IInstancePropertyOwner.Metadata => Metadata;
    string IInstancePropertyOwner.Name => Metadata.PropertyName;

    public override string ToString() {
        return $"{Metadata.PropertyName}(InstanceStructureProperty)";
    }
}

// ------------------------------------------

public static partial class CodeGeneratingHelperExtensions {

    #region CreateProperties系メソッド
    public static IInstanceProperty CreateProperty(this IInstancePropertyOwner owner, IInstancePropertyMetadata propertyMetadata) {
        return propertyMetadata switch {
            IInstanceValuePropertyMetadata v => owner.CreateProperty(v),
            IInstanceStructurePropertyMetadata s => owner.CreateProperty(s),
            _ => throw new InvalidOperationException("上記2種以外はありえない"),
        };
    }
    /// <summary>
    /// この構造体のプロパティを定義します。
    /// レンダリング処理のパフォーマンスのため、引数のプロパティがこの構造体で定義されているか否かのチェックは行なっていないので注意。
    /// </summary>
    public static InstanceValueProperty CreateProperty(this IInstancePropertyOwner owner, IInstanceValuePropertyMetadata propertyMetadata) {
        return new InstanceValueProperty {
            Root = owner switch {
                Variable v => v,
                InstanceStructureProperty s => s.Root,
                _ => throw new NotImplementedException(),
            },
            Owner = owner,
            Metadata = propertyMetadata,
        };
    }
    /// <summary>
    /// この構造体のプロパティを定義します。
    /// レンダリング処理のパフォーマンスのため、引数のプロパティがこの構造体で定義されているか否かのチェックは行なっていないので注意。
    /// </summary>
    public static InstanceStructureProperty CreateProperty(this IInstancePropertyOwner owner, IInstanceStructurePropertyMetadata propertyMetadata) {
        return new InstanceStructureProperty {
            Root = owner switch {
                Variable v => v,
                InstanceStructureProperty s => s.Root,
                _ => throw new NotImplementedException(),
            },
            Owner = owner,
            Metadata = propertyMetadata,
        };
    }

    /// <summary>
    /// この構造体のプロパティを列挙します。
    /// </summary>
    public static IEnumerable<IInstanceProperty> CreateProperties(this IInstancePropertyOwner owner) {
        foreach (var propertyMetadata in owner.Metadata.GetMembers()) {
            yield return owner.CreateProperty(propertyMetadata);
        }
    }

    /// <summary>
    /// この構造体およびその子孫のプロパティを再帰的に列挙します。
    /// </summary>
    public static IEnumerable<IInstanceProperty> CreatePropertiesRecursively(this IInstancePropertyOwner owner) {
        foreach (var prop in owner.CreateProperties()) {
            yield return prop;

            if (prop is InstanceStructureProperty structureProperty) {
                foreach (var vp in structureProperty.CreatePropertiesRecursively()) {
                    yield return vp;
                }
            }
        }
    }

    /// <summary>
    /// この構造体およびその子孫のメンバーのうち、この構造体と1対1の多重度を持つもののみを再帰的に列挙します。
    /// ToDbEntityなどのマッピングで使用。
    /// </summary>
    public static IEnumerable<IInstanceProperty> Create1To1PropertiesRecursively(this IInstancePropertyOwner owner) {
        foreach (var prop in owner.CreateProperties()) {
            yield return prop;

            // 多重度1対1のメンバーのみを列挙するためarrayでない場合は子孫を辿らない
            if (prop is InstanceStructureProperty structureProperty && !structureProperty.Metadata.IsArray) {
                foreach (var vp in structureProperty.Create1To1PropertiesRecursively()) {
                    yield return vp;
                }
            }
        }
    }
    #endregion CreateProperties系メソッド

    /// <summary>
    /// このプロパティの、大元の変数からのパス情報を返します。
    /// 大元の変数に近い方が先に列挙されます。
    /// </summary>
    public static IEnumerable<IInstanceProperty> GetPathFromInstance(this IInstanceProperty property) {
        var stack = new Stack<IInstanceProperty>();
        var current = property;
        while (true) {
            if (current is not IInstanceProperty prop) break;
            stack.Push(prop);

            if (prop.Owner is not IInstanceProperty owner) {
                break;
            }
            current = owner;
        }
        while (stack.TryPop(out var path)) {
            yield return path;
        }
    }

    /// <summary>
    /// メンバーを再帰的に列挙します。
    /// </summary>
    public static IEnumerable<IInstancePropertyMetadata> GetMetadataRecursively(this IInstancePropertyOwnerMetadata ownerMetadata) {
        foreach (var member in ownerMetadata.GetMembers()) {
            yield return member;

            if (member is IInstancePropertyOwnerMetadata owner) {
                foreach (var member2 in owner.GetMetadataRecursively()) {
                    yield return member2;
                }
            }
        }
    }

    #region GetPath系メソッド
    /// <summary>
    /// このインスタンスの、大元のインスタンスからのパスを列挙します。
    /// <c>x.Prop1?.Prop2?.Prop3?.Prop4</c> のような数珠つなぎのソースコードのレンダリングに使用します。
    /// </summary>
    /// <param name="nullableSeparator">null許容メンバーのパスの結合に使われる。 "?." または "!." を代入</param>
    public static string GetJoinedPathFromInstance(this IInstanceProperty property, E_CsTs csts, string nullableSeparator = ".") {
        var path = new StringBuilder();
        var previousIsArray = false;
        var select = csts == E_CsTs.CSharp ? "Select" : "map";
        var selectMany = csts == E_CsTs.CSharp ? "SelectMany" : "flatMap";

        // パス作成（変数部分）
        path.Append(property.Root.Name);

        // パス作成（プロパティ部分）
        var previous = (IInstanceProperty?)null;
        foreach (var current in property.GetPathFromInstance()) {
            // セパレータ
            if (previous == null) {
                // previousは変数
                path.Append('.');
            } else if (previous.IsNullable) {
                // nullの可能性があるメンバーの場合
                path.Append(nullableSeparator);
            } else {
                // nullの可能性が無いメンバーの場合
                path.Append('.');
            }

            // このメンバーの多重度がひとつ前のインスタンスに対して1対多か否か
            var currentIsArray = current is InstanceStructureProperty structureProperty && structureProperty.Metadata.IsArray;

            // メンバー名
            if (previousIsArray) {
                path.Append(currentIsArray
                    ? $"{selectMany}(e => e.{current.Metadata.PropertyName})"
                    : $"{select}(e => e.{current.Metadata.PropertyName})");
            } else {
                path.Append(current.Metadata.PropertyName);
            }

            // このメンバーが1対多なら以降のパスは Select, SelectMany（JSの場合は map, flatMap）になる
            if (!previousIsArray && currentIsArray) {
                previousIsArray = true;
            }
        }
        return path.ToString();
    }

    /// <summary>
    /// 大元の変数より後ろのパスをフラットな配列にして返す。
    /// C#はLinqの Select, SelectMany を使う。TypeScriptはmap, flatMapを使う。
    ///
    /// <code>
    /// // C#の場合の戻り値の例のイメージ
    /// .Child1.Child2.Children3.Select(x => x.Child4).SelectMany(x => x.Children5)
    ///
    /// // TypeScriptの場合の戻り値の例のイメージ
    /// .Child1.Child2.Children3.map(x => x.Child4).flatMap(x => x.Children5)
    /// </code>
    /// </summary>
    /// <param name="csts">C# or TypeScript</param>
    /// <param name="isMany">大元の変数との多重度。パスの途中にChildrenが含まれていたか否か。</param>
    public static string[] GetFlattenArrayPath(this IInstanceProperty property, E_CsTs csts, out bool isMany) {
        var path = new List<string>();
        isMany = false;

        // パスを構築
        foreach (var prop in property.GetPathFromInstance()) {

            // 配列の場合は多重度を考慮
            if (prop.Metadata is IInstanceStructurePropertyMetadata structMeta && structMeta.IsArray) {

                if (isMany) {
                    path.Add(csts == E_CsTs.CSharp
                        ? $"SelectMany(x => x.{prop.Metadata.PropertyName})"
                        : $"flatMap(x => x.{prop.Metadata.PropertyName})");
                } else {
                    path.Add(prop.Metadata.PropertyName);
                }

                isMany = true;

            } else if (isMany) {
                // 単一のプロパティかつここまでに配列が登場している場合
                path.Add(csts == E_CsTs.CSharp
                    ? $"Select(x => x.{prop.Metadata.PropertyName})"
                    : $"map(x => x.{prop.Metadata.PropertyName})");

            } else {
                // 単一のプロパティかつここまでに配列が登場していない場合
                path.Add(prop.Metadata.PropertyName);
            }
        }

        return path.ToArray();
    }
    /// <inheritdoc cref="GetFlattenArrayPath(IInstanceProperty, E_CsTs, out bool)"/>
    public static string[] GetFlattenArrayPath(this IInstancePropertyOwner owner, E_CsTs csts, out bool isMany) {
        if (owner is IInstanceProperty prop) {
            return prop.GetFlattenArrayPath(csts, out isMany);
        } else {
            // ownerがVariableの場合
            isMany = false;
            return [];
        }
    }
    #endregion GetPath系メソッド
}
