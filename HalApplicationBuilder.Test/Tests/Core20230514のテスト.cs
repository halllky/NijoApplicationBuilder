using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace HalApplicationBuilder.Test.Tests {
    public class Core20230514のテスト {
        [Fact]
        public void Test1() {

            var xDocument = XDocument.Parse(RDRA_XML.Trim());

            var successToCreateBuidler = Core20230514.AppSchemaBuilder.FromXml(xDocument, out var builder, out var errors);
            var successToBuildSchema = builder.TryBuild(out var appSchema, out var errors1);

            Assert.True(successToCreateBuidler);
            Assert.True(successToBuildSchema);

            var entries = appSchema.AllAggregates().ToArray();
            Assert.Equal(2, entries.Length);
        }

        private const string RDRA_XML = @"
<?xml version=""1.0"" encoding=""UTF-8"" ?>

<!--アプリケーション名を変更してください。-->
<YourApplicationName>
  <!-- システム価値 -->
  <要求>
    <ID type=""id"" key="""" />
    <詳細 type=""sentence"" />
    <根拠 type=""sentence"" />
  </要求>
  <アクター>
    <ID type=""id"" key="""" />
    <アクター名 type=""word"" key="""" name="""" />
  </アクター>

  <!-- システム外部境界 -->
  <ビジネスユースケース>
    <ID type=""id"" key="""" />
    <説明 type=""sentence"" name="""" />
    <アクター refTo=""/アクター"" />
  </ビジネスユースケース>

  <!-- システム境界 -->
  <画面>
    <ID type=""id"" key="""" />
    <画面名 type=""word"" key="""" name="""" />
  </画面>
  <システムユースケース>
    <ID type=""id"" key="""" />
    <説明 type=""sentence"" name="""" />
    <BUC refTo=""/ビジネスユースケース"" />
  </システムユースケース>
  <イベント>
    <ID type=""id"" key="""" />
    <イベント名称 type=""word"" key="""" name="""" />
  </イベント>

  <!-- システム -->
  <機能>
    <ID type=""id"" key="""" />
    <機能名 type=""word"" key="""" name="""" />
    <更新か参照か type=""word"" key="""" name="""" />
  </機能>
</YourApplicationName>

";
    }
}
