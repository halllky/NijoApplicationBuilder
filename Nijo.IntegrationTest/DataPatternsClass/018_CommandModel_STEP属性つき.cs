using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Nijo.IntegrationTest.DataPatternsClass {
    public class _018_CommandModel_STEP属性つき : DataPattern {
        public _018_CommandModel_STEP属性つき() : base("018_CommandModel_STEP属性つき.xml") { }

        protected override string AppTsxCustomizer() {
            return $$"""
                {
                  Into従業員MultiViewHeader: ({ getSelectedItems }) => {
                    const openCommandDialog = Comps.use従業員データ一括取り込みDialog()
                    const handleClick = useEvent(() => {
                      const items = getSelectedItems()
                      openCommandDialog({ これは最初の画面です: { 取込ファイル: items.map(x => x.values.名前).join(',') } })
                    })
                    return (
                      <Input.IconButton onClick={handleClick}>コマンド</Input.IconButton>
                    )
                  },
                  CustomUiComponent: {
                    部署検索条件Dialog: ({ value, onChange, readOnly }) => {
                      const openDialog = Comps.use部署SearchDialog()
                      const handleClick = useEvent(() => {
                        openDialog({
                          onSelect: selectedItem => onChange({
                            部署ID: selectedItem?.部署ID,
                            部署名: selectedItem?.部署名,
                          })
                        })
                      })
                      return (
                        <div className="flex gap-1">
                          <Input.Word className="w-8" value={value?.部署ID} />
                          {!readOnly && (
                            <Input.IconButton icon={Icon.MagnifyingGlassIcon} onClick={handleClick} outline mini hideText>検索</Input.IconButton>
                          )}
                          <Input.Word className="flex-1" value={value?.部署名} readOnly />
                        </div>
                      )
                    },
                    部署Dialog: ({ value, onChange, readOnly }) => {
                      const openDialog = Comps.use部署SearchDialog()
                      const handleClick = useEvent(() => {
                        openDialog({ onSelect: selectedItem => onChange(selectedItem) })
                      })
                      return (
                        <div className="flex gap-1">
                          <Input.Word className="w-8" value={value?.部署ID} />
                          {!readOnly && (
                            <Input.IconButton icon={Icon.MagnifyingGlassIcon} onClick={handleClick} outline mini hideText>検索</Input.IconButton>
                          )}
                          <Input.Word className="flex-1" value={value?.部署名} readOnly />
                        </div>
                      )
                    },
                  },
                }
                """;
        }
    }
}
