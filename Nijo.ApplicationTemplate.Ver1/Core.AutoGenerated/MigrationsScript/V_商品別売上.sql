CREATE VIEW V_商品別売上 AS
SELECT
    STRFTIME('%Y/%m', T1.ORDER_DATE) AS 売上分析_年月,
    T1."店舗_STORE_ID" AS 売上分析_店舗_店舗ID,
    T3."カテゴリ_CATEGORY_ID" AS カテゴリ_カテゴリID,
    T2."商品_PRODUCT_ID" AS 商品_ID,
    T3.PRODUCT_NAME AS 商品_商品名,
    T3.PRICE AS 商品_価格,
    T3."カテゴリ_CATEGORY_ID" AS 商品_カテゴリ_カテゴリID,
    T4.CATEGORY_NAME AS 商品_カテゴリ_カテゴリ名,
    T3."仕入先_SUPPLIER_ID" AS 商品_仕入先_仕入先ID,
    S.SUPPLIER_NAME AS 商品_仕入先_仕入先名,
    S.CONTACT_PERSON AS 商品_仕入先_担当者名,
    S.PHONE AS 商品_仕入先_電話番号,
    S.EMAIL AS 商品_仕入先_メールアドレス,
    PD.DESCRIPTION AS 商品_商品詳細_説明文,
    PS.WEIGHT AS 商品_商品詳細_商品仕様_重量,
    SZ.WIDTH AS 商品_商品詳細_商品仕様_サイズ_幅,
    SZ.HEIGHT AS 商品_商品詳細_商品仕様_サイズ_高さ,
    SZ.DEPTH AS 商品_商品詳細_商品仕様_サイズ_奥行,
    SUM(T2.SUBTOTAL) AS 売上金額,
    SUM(T2.QUANTITY) AS 売上数量,
    CASE WHEN SUM(T2.QUANTITY) = 0 THEN 0 ELSE SUM(T2.SUBTOTAL) * 1.0 / SUM(T2.QUANTITY) END AS 平均単価
FROM
    注文履歴 AS T1
    INNER JOIN ORDER_DETAILS AS T2 ON T1.ORDER_ID = T2.Parent_ORDER_ID
    INNER JOIN 商品マスタ AS T3 ON T2."商品_PRODUCT_ID" = T3.PRODUCT_ID
    LEFT JOIN カテゴリマスタ AS T4 ON T3."カテゴリ_CATEGORY_ID" = T4.CATEGORY_ID
    LEFT JOIN 仕入先マスタ AS S ON T3."仕入先_SUPPLIER_ID" = S.SUPPLIER_ID
    LEFT JOIN PRODUCT_DETAIL AS PD ON T3.PRODUCT_ID = PD.Parent_PRODUCT_ID
    LEFT JOIN PRODUCT_SPEC AS PS ON PD.Parent_PRODUCT_ID = PS.Parent_Parent_PRODUCT_ID
    LEFT JOIN SIZE AS SZ ON PS.Parent_Parent_PRODUCT_ID = SZ.Parent_Parent_Parent_PRODUCT_ID
    LEFT JOIN ACCESSORIES AS ACC ON PD.Parent_PRODUCT_ID = ACC.Parent_Parent_PRODUCT_ID
    LEFT JOIN INVENTORY AS INV ON T3.PRODUCT_ID = INV.Parent_PRODUCT_ID
GROUP BY
    STRFTIME('%Y/%m', T1.ORDER_DATE),
    T1."店舗_STORE_ID",
    T3."カテゴリ_CATEGORY_ID",
    T2."商品_PRODUCT_ID",
    T3.PRODUCT_NAME,
    T3.PRICE,
    T4.CATEGORY_NAME,
    T3."仕入先_SUPPLIER_ID",
    S.SUPPLIER_NAME,
    S.CONTACT_PERSON,
    S.PHONE,
    S.EMAIL,
    PD.DESCRIPTION,
    PS.WEIGHT,
    SZ.WIDTH,
    SZ.HEIGHT,
    SZ.DEPTH; 