CREATE VIEW V_時間帯別売上 AS
SELECT
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE) AS 売上分析_年月,
    注文履歴.STORE_ID AS 売上分析_店舗_店舗ID,
    SUBSTR('00' || STRFTIME('%H', 注文履歴.ORDER_DATE), -2) || ':00-' || SUBSTR('00' || STRFTIME('%H', 注文履歴.ORDER_DATE), -2) || ':59' AS 時間帯,
    SUM(注文明細.SUBTOTAL) AS 売上金額,
    COUNT(注文履歴.ORDER_ID) AS 売上件数,
    CASE WHEN COUNT(注文履歴.ORDER_ID) = 0 THEN 0 ELSE SUM(注文明細.SUBTOTAL) * 1.0 / COUNT(注文履歴.ORDER_ID) END AS 平均客単価
FROM
    注文履歴
    INNER JOIN 注文明細 ON 注文履歴.ORDER_ID = 注文明細.Parent_ORDER_ID
GROUP BY
    STRFTIME('%Y/%m', 注文履歴.ORDER_DATE),
    注文履歴.STORE_ID,
    STRFTIME('%H', 注文履歴.ORDER_DATE); 