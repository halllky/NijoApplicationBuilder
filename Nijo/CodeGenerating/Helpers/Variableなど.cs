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
    bool IsArray { get; }
}

/// <summary>
/// <see cref="IInstanceValuePropertyMetadata"/> or <see cref="IInstanceStructurePropertyMetadata"/>
/// </summary>
public interface IInstancePropertyMetadata {
    SchemaNodeIdentity MappingKey { get; }
    string PropertyName { get; }
}

// ------------------------------------------
// メタデータの抽象度の世界より1段階具体化された世界（自動生成されたあとのソースコードの抽象度の世界）: インタフェース

/// <summary>
/// 自動生成されるソースコード上に表れるインスタンスのプロパティ。
/// オーナーへの参照を持つ。
/// </summary>
public interface IInstanceProperty {
    SchemaNodeIdentity MappingKey { get; }
    IInstancePropertyOwner Owner { get; }
    string PropertyName { get; }
    bool IsNullable { get; }
}
/// <summary>
/// <see cref="IInstanceProperty"/> のオーナー
/// </summary>
public interface IInstancePropertyOwner {
    string Name { get; }
}

// ------------------------------------------
// メタデータの抽象度の世界より1段階具体化された世界（自動生成されたあとのソースコードの抽象度の世界）: 具象クラス

/// <summary>
/// 自動生成されるソースコードの中に表れる変数
/// </summary>
public sealed class Variable : IInstancePropertyOwner {
    public Variable(string name) {
        Name = name;
    }
    public string Name { get; }
}
/// <summary>
/// 自動生成されるソースコードの中に表れる変数のプロパティのうち、子孫をもたない値メンバーのプロパティ。
/// </summary>
public sealed class InstanceValueProperty : IInstanceProperty {
    public required SchemaNodeIdentity MappingKey { get; init; }
    public required IInstancePropertyOwner Owner { get; init; }
    public required string PropertyName { get; init; }
    public required IValueMemberType Type { get; init; }
    public bool IsNullable => true;
}
/// <summary>
/// 自動生成されるソースコードの中に表れる変数のプロパティのうち、子孫をもつ構造体メンバーのプロパティ。
/// </summary>
public sealed class InstanceStructureProperty : IInstanceProperty, IInstancePropertyOwner {
    public required SchemaNodeIdentity MappingKey { get; init; }
    public required IInstancePropertyOwner Owner { get; init; }
    public required string PropertyName { get; init; }
    public required bool IsArray { get; init; }
    public bool IsNullable => true;

    string IInstancePropertyOwner.Name => PropertyName;
}

// ------------------------------------------

public static partial class CodeGeneratingHelperExtensions {

    /// <summary>
    /// メンバーを再帰的に列挙します。
    /// </summary>
    public static IEnumerable<IInstancePropertyMetadata> GetMembersRecursively(this IInstancePropertyOwnerMetadata ownerMetadata) {
        foreach (var member in ownerMetadata.GetMembers()) {
            yield return member;

            if (member is IInstancePropertyOwnerMetadata owner) {
                foreach (var member2 in owner.GetMembersRecursively()) {
                    yield return member2;
                }
            }
        }
    }

    /// <summary>
    /// このインスタンスの、大元のインスタンスからのパスを列挙します。
    /// <c>x.Prop1?.Prop2?.Prop3?.Prop4</c> のような数珠つなぎのソースコードのレンダリングに使用します。
    /// </summary>
    /// <param name="nullableSeparator">null許容メンバーのパスの結合に使われる。 "?." または "!." を代入</param>
    public static string GetJoinedPathFromInstance(this IInstanceProperty terminal, string nullableSeparator = ".") {
        var stack = new Stack<IInstanceProperty>();
        var current = terminal;
        var rootVariable = (Variable?)null;
        while (true) {
            if (current is not IInstanceProperty prop) break;
            stack.Push(prop);

            if (prop.Owner is not IInstanceProperty owner) {
                rootVariable = prop.Owner as Variable;
                break;
            }
            current = owner;
        }

        // パス作成（変数部分）
        var path = new StringBuilder();
        path.Append(rootVariable?.Name ?? throw new InvalidOperationException("大元は必ず変数のはずなのでこの分岐にくるのはありえない"));

        // パス作成（プロパティ部分）
        var previous = (IInstanceProperty?)null;
        while (stack.TryPop(out var prop)) {
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

            // メンバー名
            path.Append(prop.PropertyName);

            previous = prop;
        }
        return path.ToString();
    }

    /// <summary>
    /// この構造体の子孫のメンバーのうち、この構造体と1対1の多重度を持つもののみを再帰的に列挙します。
    /// ToDbEntityなどのマッピングで使用。
    /// </summary>
    public static IEnumerable<IInstanceProperty> Get1To1ValuePropertiesRecursively(this IInstancePropertyOwnerMetadata ownerMetadata, IInstancePropertyOwner owner) {
        foreach (var member in ownerMetadata.GetMembers()) {
            if (member is IInstanceValuePropertyMetadata valueMetadata) {
                yield return new InstanceValueProperty {
                    MappingKey = valueMetadata.MappingKey,
                    Owner = owner,
                    PropertyName = valueMetadata.PropertyName,
                    Type = valueMetadata.Type,
                };

            } else if (member is IInstanceStructurePropertyMetadata structMetadata) {
                var prop = new InstanceStructureProperty {
                    MappingKey = structMetadata.MappingKey,
                    Owner = owner,
                    PropertyName = structMetadata.PropertyName,
                    IsArray = structMetadata.IsArray,
                };

                yield return prop;

                // 多重度1対1のメンバーのみを列挙するためarrayでない場合は子孫を辿らない
                if (!structMetadata.IsArray) {
                    foreach (var vp in structMetadata.Get1To1ValuePropertiesRecursively(prop)) {
                        yield return vp;
                    }
                }

            } else {
                throw new NotImplementedException("上2種類以外はありえない");
            }
        }
    }
}
