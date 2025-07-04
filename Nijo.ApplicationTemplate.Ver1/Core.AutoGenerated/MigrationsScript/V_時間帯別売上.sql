CREATE VIEW V_時間帯別売上 AS
SELECT
    CAST(STRFTIME('%Y%m', T1.ORDER_DATE) AS INTEGER) AS 売上分析_年月,
    T1."店舗_STORE_ID" AS 売上分析_店舗_店舗ID,
    SUBSTR('00' || STRFTIME('%H', T1.ORDER_DATE), -2) || ':00-' || SUBSTR('00' || STRFTIME('%H', T1.ORDER_DATE), -2) || ':59' AS 時間帯,
    SUM(T2.SUBTOTAL) AS 売上金額,
    COUNT(T1.ORDER_ID) AS 売上件数,
    CASE WHEN COUNT(T1.ORDER_ID) = 0 THEN 0 ELSE SUM(T2.SUBTOTAL) * 1.0 / COUNT(T1.ORDER_ID) END AS 平均客単価
FROM
    注文履歴 AS T1
    INNER JOIN ORDER_DETAILS AS T2 ON T1.ORDER_ID = T2.Parent_ORDER_ID
GROUP BY
    CAST(STRFTIME('%Y%m', T1.ORDER_DATE) AS INTEGER),
    T1."店舗_STORE_ID",
    STRFTIME('%H', T1.ORDER_DATE); 