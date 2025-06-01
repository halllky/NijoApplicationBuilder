CREATE VIEW V_商品別売上 AS
SELECT
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE) AS カテゴリ別売上_売上分析_年月,
    注文履歴.STORE_ID AS カテゴリ別売上_売上分析_店舗_店舗ID,
    商品マスタ.CATEGORY_ID AS カテゴリ別売上_カテゴリ_カテゴリID,
    注文明細.PRODUCT_ID AS 商品_ID,
    SUM(注文明細.SUBTOTAL) AS 売上金額,
    SUM(注文明細.QUANTITY) AS 売上数量,
    CASE WHEN SUM(注文明細.QUANTITY) = 0 THEN 0 ELSE SUM(注文明細.SUBTOTAL) * 1.0 / SUM(注文明細.QUANTITY) END AS 平均単価
FROM
    注文明細
    INNER JOIN 注文履歴 ON 注文明細.Parent_ORDER_ID = 注文履歴.ORDER_ID
    INNER JOIN 商品マスタ ON 注文明細.PRODUCT_ID = 商品マスタ.PRODUCT_ID
GROUP BY
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE),
    注文履歴.STORE_ID,
    商品マスタ.CATEGORY_ID,
    注文明細.PRODUCT_ID; 