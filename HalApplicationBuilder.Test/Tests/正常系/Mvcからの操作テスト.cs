﻿using System;
using Xunit;
using OpenQA.Selenium;

namespace HalApplicationBuilder.Test.Tests.正常系 {
    public class Mvc経由の操作テスト {

        [Fact]
        public void 登録検索更新削除() {
            using var web = DistMvcProject.Instance
                .GenerateCode(typeof(_20221210試用版.商品).Namespace)
                .BuildProject()
                .RunWebProcess();
            using var driver = web.GetFireFoxDriver();

            // トップページ
            var shohinLink = driver.FindElement(By.LinkText("商品"));
            shohinLink.Click();

            // 商品一覧画面
            var newPageLink = driver.FindElement(By.LinkText("新規作成"));
            newPageLink.Click();

            // 商品新規作成画面
            var shohinCdTextBox = driver.FindElement(By.Name("Item.商品コード"));
            var shohinNameTextBox = driver.FindElement(By.Name("Item.商品名"));
            var priceTextBox = driver.FindElement(By.Name("Item.単価"));
            shohinCdTextBox.SendKeys("001");
            shohinNameTextBox.SendKeys("商品001");
            priceTextBox.Clear();
            priceTextBox.SendKeys("1");

            var createButton = driver.FindElement(By.XPath("//*[text()=\"作成\"]"));
            createButton.Click();

            // 商品詳細画面（新規登録後）
            Assert.Equal("001", driver.FindElement(By.Name("Item.商品コード")).GetAttribute("value"));
            Assert.Equal("商品001", driver.FindElement(By.Name("Item.商品名")).GetAttribute("value"));
            Assert.Equal("1", driver.FindElement(By.Name("Item.単価")).GetAttribute("value"));

            var updateButton = driver.FindElement(By.XPath("//*[text()=\"更新\"]"));
            updateButton.Click();

            // 商品詳細画面（更新後）
            Assert.Equal("001", driver.FindElement(By.Name("Item.商品コード")).GetAttribute("value"));
            Assert.Equal("商品001", driver.FindElement(By.Name("Item.商品名")).GetAttribute("value"));
            Assert.Equal("1", driver.FindElement(By.Name("Item.単価")).GetAttribute("value"));

            driver.FindElement(By.LinkText("商品")).Click();

            // 商品一覧画面
            var searchButton = driver.FindElement(By.XPath("//*[text()=\"検索\"]"));
            searchButton.Click();

            var table = driver.FindElement(By.TagName("table"));

            var headerCells = table
                .FindElement(By.TagName("thead"))
                .FindElement(By.TagName("tr"))
                .FindElements(By.TagName("th"));
            Assert.Equal("", headerCells[0].Text);
            Assert.Equal("商品コード", headerCells[1].Text);
            Assert.Equal("商品名", headerCells[2].Text);
            Assert.Equal("単価", headerCells[3].Text);

            var bodyCells = table
                .FindElement(By.TagName("tbody"))
                .FindElement(By.TagName("tr"))
                .FindElements(By.TagName("td"));
            Assert.Equal("詳細", bodyCells[0].Text);
            Assert.Equal("001", bodyCells[1].Text);
            Assert.Equal("商品001", bodyCells[2].Text);
            Assert.Equal("1", bodyCells[3].Text);
        }
    }
}
