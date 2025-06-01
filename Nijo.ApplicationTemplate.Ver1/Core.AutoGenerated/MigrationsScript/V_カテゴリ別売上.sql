CREATE VIEW V_カテゴリ別売上 AS
SELECT
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE) AS 売上分析_年月,
    注文履歴.STORE_ID AS 売上分析_店舗_店舗ID,
    商品マスタ.CATEGORY_ID AS カテゴリ_カテゴリID,
    SUM(注文明細.SUBTOTAL) AS 売上金額,
    CAST(SUM(注文明細.SUBTOTAL) AS REAL) / NULLIF((SELECT SUM(注文明細_INNER.SUBTOTAL) FROM 注文明細 注文明細_INNER INNER JOIN 注文履歴 注文履歴_INNER ON 注文明細_INNER.Parent_ORDER_ID = 注文履歴_INNER.ORDER_ID WHERE STRFTIME('%Y/%m', 注文履歴_INNER.ORDER_DATE) = STRFTIME('%Y/%m', 注文履歴.ORDER_DATE) AND 注文履歴_INNER.STORE_ID = 注文履歴.STORE_ID), 0) AS 売上構成比
FROM
    注文明細
    INNER JOIN 注文履歴 ON 注文明細.Parent_ORDER_ID = 注文履歴.ORDER_ID
    INNER JOIN 商品マスタ ON 注文明細.PRODUCT_ID = 商品マスタ.PRODUCT_ID
GROUP BY
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE),
    注文履歴.STORE_ID,
    商品マスタ.CATEGORY_ID; 