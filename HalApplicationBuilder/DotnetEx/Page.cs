using System;
namespace HalApplicationBuilder.DotnetEx {
    public sealed class Page {
        public Page(int number, int size) {
            if (number < 1) throw new ArgumentException($"Page number is less than 1.", nameof(number));
            if (size < 1) throw new ArgumentException($"Page size is less than 1.", nameof(size));
            No = number;
            Size = size;
        }

        public int No { get; }
        public int Size { get; }

        public int SqlOffset => (No - 1) * Size;
        public int SqlLimit => Size;
    }
}
