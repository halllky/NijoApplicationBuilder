using System;
namespace Nijo.DotnetEx {

    /// <summary>
    /// 任意の文字列から生成されたGUID。復号はできない。
    /// </summary>
    public class HashedString {
        public HashedString(string value) {
            byte[] stringBytes = System.Text.Encoding.UTF8.GetBytes(value);
            byte[] hashedBytes = System.Security.Cryptography.MD5.Create().ComputeHash(stringBytes);
            byte[] guidBytes = new byte[16];
            Array.Copy(hashedBytes, 0, guidBytes, 0, 16);
            Guid = new Guid(guidBytes);
        }

        public Guid Guid { get; }

        public string ToCSharSafe() {
            return Guid.ToString().Replace("-", "");
        }
        public string ToFileSafe() {
            return Guid.ToString();
        }
    }
}

