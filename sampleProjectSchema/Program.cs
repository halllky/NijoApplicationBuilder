﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace haldoc {
    class Program {
        static void Main(string[] args) {
            Console.WriteLine("コード自動生成 開始");

            var context = new Core.ProjectContext(
                "サンプルプロジェクト",
                System.Reflection.Assembly.GetExecutingAssembly());

            File.WriteAllText(
                @"../../../../haldoc/AutoGenerated/EFCode.cs",
                new CodeGenerating.EFCodeGenerator() { Context = context }.TransformText());
            File.WriteAllText(
                @"../../../../haldoc/AutoGenerated/MvcModels.cs",
                new CodeGenerating.MvcModelGenerator() { Context = context }.TransformText());

            foreach (var aggregate in context.EnumerateRootAggregates()) {
                File.WriteAllText(
                    $@"../../../../haldoc/AutoGenerated/Views/{aggregate.Name}__ListView.cshtml",
                    new CodeGenerating.ListViewGenerator() { Context = context, Aggregate = aggregate }.TransformText());

                File.WriteAllText(
                    $@"../../../../haldoc/AutoGenerated/Views/{aggregate.Name}__CreateView.cshtml",
                    new CodeGenerating.CreateViewGenerator() { Context = context, Aggregate = aggregate }.TransformText());
            }

            Console.WriteLine("コード自動生成 終了");
        }
    }
}
