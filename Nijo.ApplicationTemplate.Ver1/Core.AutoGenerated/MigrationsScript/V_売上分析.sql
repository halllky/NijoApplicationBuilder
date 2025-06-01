CREATE VIEW V_売上分析 AS
SELECT
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE) AS 年月,
    注文履歴.STORE_ID AS 店舗_店舗ID,
    SUM(注文明細.SUBTOTAL) AS 売上合計,
    COUNT(DISTINCT 注文履歴.CUSTOMER_ID) AS 客数,
    CASE WHEN COUNT(DISTINCT 注文履歴.CUSTOMER_ID) = 0 THEN 0 ELSE SUM(注文明細.SUBTOTAL) * 1.0 / COUNT(DISTINCT 注文履歴.CUSTOMER_ID) END AS 客単価
FROM
    注文履歴
    INNER JOIN 注文明細 ON 注文履歴.ORDER_ID = 注文明細.Parent_ORDER_ID
GROUP BY
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE),
    注文履歴.STORE_ID; 