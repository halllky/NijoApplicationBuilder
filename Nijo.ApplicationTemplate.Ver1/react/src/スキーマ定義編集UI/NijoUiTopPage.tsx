import React from "react"
import { Link, useOutletContext } from "react-router-dom"
import { SchemaDefinitionOutletContextType } from "./スキーマ定義編集/types"
import useEvent from "react-use-event-hook";
import { NavigationMenuItem, Perspective } from "../型つきドキュメント/types";
import { UUID } from "uuidjs";
import * as Input from "../input"
import * as Icon from "@heroicons/react/24/solid"
import { getNavigationUrl } from "../routes";

export const NijoUiTopPage = () => {

  const {
    typedDoc: {
      createPerspective,
      isReady,
      loadNavigationMenus,
      savePerspective,
    },
  } = useOutletContext<SchemaDefinitionOutletContextType>()

  // Perspective一覧
  const [perspectives, setPerspectives] = React.useState<NavigationMenuItem[]>([])
  React.useEffect(() => {
    if (isReady) {
      loadNavigationMenus().then(menus => {
        setPerspectives(menus.filter(menu => menu.type === 'perspective'))
      })
    }
  }, [isReady, loadNavigationMenus])

  // 新しいPerspectiveを追加する処理
  const handleNewPerspective = useEvent(async () => {
    const perspectiveName = prompt('新しいPerspective名を入力してください。');
    if (!perspectiveName) return;

    try {
      const newPerspective: Perspective = { // Perspectiveをインポートする必要がある
        perspectiveId: UUID.generate(),
        name: perspectiveName,
        nodes: [],
        edges: [],
        attributes: [],
      };
      await createPerspective(newPerspective);
    } catch (error) {
      console.error(error);
      alert(`エラーが発生しました: ${error instanceof Error ? error.message : String(error)}`);
    }
  });

  return (
    <div>
      {perspectives.filter(perspective => perspective.type === 'perspective').map(perspective => (
        <div key={perspective.id}>
          <Link to={getNavigationUrl({ page: 'typed-document-perspective', perspectiveId: perspective.id })}>
            {perspective.label}
          </Link>
        </div>
      ))}
      <Input.IconButton icon={Icon.PlusIcon} outline onClick={handleNewPerspective}>メモ新規作成</Input.IconButton>
      <br />
      <Link to={getNavigationUrl({ page: 'schema' })}>スキーマ定義</Link>
    </div>
  )
}