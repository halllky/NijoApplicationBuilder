using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace NIJO_APPLICATION_TEMPLATE {
    /// <summary>
    /// Entity Frameword Core 関連の処理で使用される、
    /// <see cref="FileAttachment"/> 型のプロパティと、DBのカラムの型変換。
    /// DBには添付ファイルのメタデータのJSONのみが保存される。
    /// </summary>
    internal class EFCoreFileAttachmentConverter : ValueConverter<FileAttachmentMetadata, string> {
        public EFCoreFileAttachmentConverter() : base(
            csValue => csValue.ToJson(),
            dbValue => Util.ParseJson<FileAttachmentMetadata>(dbValue)) { }
    }
}
