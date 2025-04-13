using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Nijo.CodeGenerating.Helpers;

/// <summary>
/// レンダリング後のソースコードに出てくる変数やプロパティといった何らかの実体を表す抽象。
/// </summary>
public interface IInstance {
    /// <summary>
    /// このインスタンス自身のソースコード上の名前。祖先のパスは含まない。
    /// </summary>
    string Name { get; }
    /// <summary>
    /// このインスタンスがnullになる可能性があるかどうかを返します。
    /// </summary>
    bool IsNullable { get; }
}

public interface IInstanceProperty : IInstance {
    /// <summary>このプロパティを保持しているインスタンス</summary>
    public IInstance Owner { get; }
    /// <summary>
    /// クラス間の変換処理をレンダリングする際、左辺のインスタンスと対応する右辺のインスタンスがそのコンテキスト中のどれかを特定する必要がある。
    /// その特定に使われるキー。
    /// </summary>
    SchemaNodeIdentity Key { get; }
}

/// <summary>
/// レンダリング後のソースコードに出てくる変数を表す。
/// </summary>
public class Variable : IInstance {
    public Variable(string name, Func<IEnumerable<InstancePropertyWithoutOwner>> getProperties) {
        VariableName = name;
        GetProperties = getProperties;
    }

    /// <summary>変数の名前</summary>
    public string VariableName { get; }
    string IInstance.Name => VariableName;

    public bool IsNullable => false;
    public Func<IEnumerable<InstancePropertyWithoutOwner>> GetProperties { get; }
}

/// <summary>
/// レンダリング後のソースコードに出てくる変数やプロパティのメンバーであるプロパティを表す。
/// これ自身はさらに子孫要素をもたない。
/// </summary>
public class ValueProperty : IInstanceProperty {
    public ValueProperty(IInstance owner, InstancePropertyWithoutOwner propInfo) {
        if (propInfo.GetProperties != null) throw new ArgumentException();
        if (propInfo.IsMany) throw new ArgumentException();

        Owner = owner;
        PropertyName = propInfo.PropertyName;
        IsNullable = propInfo.IsNullable;
        Key = propInfo.Key;
    }

    /// <summary>このプロパティを保持しているインスタンス</summary>
    public IInstance Owner { get; }
    /// <summary>プロパティ名</summary>
    public string PropertyName { get; }
    string IInstance.Name => PropertyName;

    public bool IsNullable { get; }
    public SchemaNodeIdentity Key { get; }
}

/// <summary>
/// レンダリング後のソースコードに出てくる変数やプロパティのメンバーであるプロパティを表す。
/// さらに子要素をもつオブジェクト。
/// 親との多重度は「親 : 子 = 1 : (0 or 1)」
/// </summary>
public class ContainerProperty : IInstanceProperty {
    public ContainerProperty(IInstance owner, InstancePropertyWithoutOwner propInfo) {
        if (propInfo.GetProperties == null) throw new ArgumentException();
        if (propInfo.IsMany) throw new ArgumentException();

        Owner = owner;
        PropertyName = propInfo.PropertyName;
        IsNullable = propInfo.IsNullable;
        Key = propInfo.Key;
        GetProperties = propInfo.GetProperties;
    }

    /// <summary>このプロパティを保持しているインスタンス</summary>
    public IInstance Owner { get; }
    /// <summary>プロパティ名</summary>
    public string PropertyName { get; }
    string IInstance.Name => PropertyName;

    public bool IsNullable { get; }
    public SchemaNodeIdentity Key { get; }
    public Func<IEnumerable<InstancePropertyWithoutOwner>> GetProperties { get; }
}

/// <summary>
/// レンダリング後のソースコードに出てくる変数やプロパティのメンバーであるプロパティを表す。
/// 配列。
/// 親との多重度は「親 : 子 = 1 : N」
/// </summary>
public class ArrayProperty : IInstanceProperty {
    public ArrayProperty(IInstance owner, InstancePropertyWithoutOwner propInfo) {
        if (propInfo.GetProperties == null) throw new ArgumentException();
        if (!propInfo.IsMany) throw new ArgumentException();

        Owner = owner;
        PropertyName = propInfo.PropertyName;
        IsNullable = propInfo.IsNullable;
        Key = propInfo.Key;
        GetProperties = propInfo.GetProperties;
    }

    /// <summary>このプロパティを保持しているインスタンス</summary>
    public IInstance Owner { get; }
    /// <summary>プロパティ名</summary>
    public string PropertyName { get; }
    string IInstance.Name => PropertyName;

    public bool IsNullable { get; }
    public SchemaNodeIdentity Key { get; }
    public Func<IEnumerable<InstancePropertyWithoutOwner>> GetProperties { get; }
}

/// <summary>
/// <see cref="ContainerProperty"/> の初期化に用いられる
/// </summary>
public class InstancePropertyWithoutOwner {
    public required string PropertyName { get; init; }
    public required bool IsNullable { get; init; }
    public required SchemaNodeIdentity Key { get; init; }
    public required bool IsMany { get; init; }
    public required Func<IEnumerable<InstancePropertyWithoutOwner>>? GetProperties { get; init; }
}


public static partial class CodeGeneratingHelperExtensions {

    /// <summary>
    /// このインスタンスの、大元のインスタンスからのパスを列挙します。
    /// <c>x.Prop1?.Prop2?.Prop3?.Prop4</c> のような数珠つなぎのソースコードのレンダリングに使用します。
    /// </summary>
    /// <param name="nullableSeparator">null許容メンバーのパスの結合に使われる。 "?." または "!." を代入</param>
    public static string GetJoinedPathFromInstance(this IInstance instance, string nullableSeparator = ".") {
        var stack = new Stack<IInstance>();
        var current = instance;
        while (current is IInstanceProperty property) {
            stack.Push(property);
            current = property.Owner;
        }
        stack.Push(current);

        // パスの結合
        var path = new StringBuilder();
        var previous = (IInstance?)null;
        while (stack.TryPop(out var node)) {
            // セパレータ
            if (previous == null) {
                // 最初のインスタンスなのでセパレータなし
            } else if (previous.IsNullable) {
                // nullの可能性があるメンバーの場合
                path.Append(nullableSeparator);
            } else {
                // nullの可能性が無いメンバーの場合
                path.Append('.');
            }

            // メンバー名
            path.Append(node.Name);

            previous = node;
        }
        return path.ToString();
    }

    /// <summary>
    /// この変数のメンバーのうち、この変数との多重度が1対0または1対1になるものを再帰的に列挙します。
    /// </summary>
    /// <remarks>
    /// 主に以下のような異なるクラス間のマッピングのコードの生成に使用
    /// <code>
    /// public XXX ConvertToXXX(YYY yyy)
    ///     return new XXX {
    ///         Prop1 = yyy.Prop1,
    ///         Prop2 = yyy.Prop2,
    ///         Children = yyy.Children.Select(c => new CCC {
    ///             Prop1 = yyy.Prop1, // 子のメンバーのうち一部だけは親のメンバーから代入される、というケースもある
    ///             Prop3 = c.Prop3,
    ///             Prop4 = c.Prop4,
    ///         }).ToList(),
    ///     };
    /// }
    /// </code>
    /// </remarks>
    public static IEnumerable<IInstanceProperty> EnumerateOneToOnePropertiesRecursively(this Variable variable) {
        foreach (var propInfo in variable.GetProperties()) {
            if (propInfo.GetProperties == null) {
                yield return new ValueProperty(variable, propInfo);

            } else if (propInfo.IsMany) {
                yield return new ArrayProperty(variable, propInfo);

            } else {
                var container = new ContainerProperty(variable, propInfo);
                yield return container;

                foreach (var instance in EnumerateRecursively(container)) {
                    yield return instance;
                }
            }
        }

        static IEnumerable<IInstanceProperty> EnumerateRecursively(ContainerProperty container) {
            foreach (var propInfo in container.GetProperties()) {
                if (propInfo.GetProperties == null) {
                    yield return new ValueProperty(container, propInfo);

                } else if (propInfo.IsMany) {
                    yield return new ArrayProperty(container, propInfo);

                } else {
                    var descendant = new ContainerProperty(container, propInfo);
                    yield return descendant;

                    foreach (var instance in EnumerateRecursively(descendant)) {
                        yield return instance;
                    }
                }
            }
        }
    }
}
