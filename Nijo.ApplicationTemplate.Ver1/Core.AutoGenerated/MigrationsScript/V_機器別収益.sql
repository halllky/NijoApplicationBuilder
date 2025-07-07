CREATE VIEW V_機器別収益 AS
SELECT
    CAST(STRFTIME('%Y%m', T1.ORDER_DATE) AS INTEGER) AS 機器分類別収益_診療収益分析_年月,
    T1."診療科_STORE_ID" AS 機器分類別収益_診療収益分析_診療科_診療科ID,
    T3."機器分類_CATEGORY_ID" AS 機器分類別収益_機器分類_機器分類ID,
    T2."医療機器_PRODUCT_ID" AS 医療機器_機器ID,
    T3.PRODUCT_NAME AS 医療機器_機器名,
    T3.PRICE AS 医療機器_単価,
    T3."機器分類_CATEGORY_ID" AS 医療機器_機器分類_機器分類ID,
    T4.CATEGORY_NAME AS 医療機器_機器分類_機器分類名,
    T3."供給業者_SUPPLIER_ID" AS 医療機器_供給業者_供給業者ID,
    S.SUPPLIER_NAME AS 医療機器_供給業者_供給業者名,
    S.CONTACT_PERSON AS 医療機器_供給業者_担当者名,
    S.PHONE AS 医療機器_供給業者_電話番号,
    S.EMAIL AS 医療機器_供給業者_メールアドレス,
    PD.DESCRIPTION AS 医療機器_機器詳細_機器説明,
    PS.WEIGHT AS 医療機器_機器詳細_機器仕様_重量,
    SZ.WIDTH AS 医療機器_機器詳細_機器仕様_サイズ_幅,
    SZ.HEIGHT AS 医療機器_機器詳細_機器仕様_サイズ_高さ,
    SZ.DEPTH AS 医療機器_機器詳細_機器仕様_サイズ_奥行,
    SUM(T2.SUBTOTAL) AS 診療収益,
    SUM(T2.QUANTITY) AS 使用回数,
    CASE WHEN SUM(T2.QUANTITY) = 0 THEN 0 ELSE SUM(T2.SUBTOTAL) * 1.0 / SUM(T2.QUANTITY) END AS 平均単価
FROM
    "診療履歴" AS T1
    INNER JOIN ORDER_DETAILS AS T2 ON T1.ORDER_ID = T2.Parent_ORDER_ID
    INNER JOIN "医療機器マスタ" AS T3 ON T2."医療機器_PRODUCT_ID" = T3.PRODUCT_ID
    LEFT JOIN "機器分類マスタ" AS T4 ON T3."機器分類_CATEGORY_ID" = T4.CATEGORY_ID
    LEFT JOIN "供給業者マスタ" AS S ON T3."供給業者_SUPPLIER_ID" = S.SUPPLIER_ID
    LEFT JOIN PRODUCT_DETAIL AS PD ON T3.PRODUCT_ID = PD.Parent_PRODUCT_ID
    LEFT JOIN PRODUCT_SPEC AS PS ON PD.Parent_PRODUCT_ID = PS.Parent_Parent_PRODUCT_ID
    LEFT JOIN SIZE AS SZ ON PS.Parent_Parent_PRODUCT_ID = SZ.Parent_Parent_Parent_PRODUCT_ID
    LEFT JOIN ACCESSORIES AS ACC ON PD.Parent_PRODUCT_ID = ACC.Parent_Parent_PRODUCT_ID
    LEFT JOIN INVENTORY AS INV ON T3.PRODUCT_ID = INV.Parent_PRODUCT_ID
GROUP BY
    CAST(STRFTIME('%Y%m', T1.ORDER_DATE) AS INTEGER),
    T1."診療科_STORE_ID",
    T3."機器分類_CATEGORY_ID",
    T2."医療機器_PRODUCT_ID",
    T3.PRODUCT_NAME,
    T3.PRICE,
    T4.CATEGORY_NAME,
    T3."供給業者_SUPPLIER_ID",
    S.SUPPLIER_NAME,
    S.CONTACT_PERSON,
    S.PHONE,
    S.EMAIL,
    PD.DESCRIPTION,
    PS.WEIGHT,
    SZ.WIDTH,
    SZ.HEIGHT,
    SZ.DEPTH; 