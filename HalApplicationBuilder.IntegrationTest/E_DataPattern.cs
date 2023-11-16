namespace HalApplicationBuilder.IntegrationTest {
    public enum E_DataPattern {

        [FileName("000_主キー1個の集約.xml")]
        _000_主キー1個の集約xml,
        [FileName("001_Refのみ.xml")]
        _001_Refのみxml,
        [FileName("002_Childrenのみ.xml")]
        _002_Childrenのみxml,
        [FileName("003_Childのみ.xml")]
        _003_Childのみxml,
        [FileName("004_Variationのみ.xml")]
        _004_Variationのみxml,

        [FileName("010_ChildrenからChildrenへの参照.xml")]
        _010_ChildrenからChildrenへの参照,
        [FileName("011_ダブル.xml")]
        _011_ダブル,
        [FileName("013_主キーにRef.xml")]
        _013_主キーにRef,

        [FileName("100_RDRA.xml")]
        _100_RDRAxml,
    }
}
