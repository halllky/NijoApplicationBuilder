import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import { PerspectivePageData, TypedDocumentComment } from './types'; // 型をインポート
import * as Input from '../../input'; // Inputコンポーネントをインポート
import * as Icon from '@heroicons/react/24/solid'; // アイコンをインポート
import { UUID } from 'uuidjs'; // UUID生成のため
import useEvent from 'react-use-event-hook';

// Propsの型定義
interface EntityDetailPaneProps {
  rhfMethods: ReactHookForm.UseFormReturn<PerspectivePageData>;
  entityIndex: number;
}

export const EntityDetailPane: React.FC<EntityDetailPaneProps> = ({
  rhfMethods,
  entityIndex,
}) => {
  const { control } = rhfMethods

  // エンティティ名を監視
  const entityName = ReactHookForm.useWatch({ name: `perspective.nodes.${entityIndex}.entityName`, control });

  // コメントのuseFieldArray
  const comments = ReactHookForm.useFieldArray<PerspectivePageData, `perspective.nodes.${number}.comments`>({
    control,
    name: `perspective.nodes.${entityIndex}.comments`,
  });

  const handleAddComment = useEvent(() => {
    const newComment: TypedDocumentComment = {
      commentId: UUID.generate(), // 新しいコメントIDを生成
      content: "",
      author: "current_user", // TODO: 実際のユーザー情報に置き換える
      createdAt: new Date().toISOString(),
    };
    comments.append(newComment);
  })

  return (
    <div className="p-4 border-l h-full flex flex-col">
      <h2 className="text-lg font-semibold mb-3 flex-none">{entityName || 'エンティティ詳細'}</h2>

      <div className="mb-4 flex-grow flex flex-col">
        <h3 className="text-md font-semibold mb-2 flex-none">コメント</h3>
        <div className="flex-grow overflow-y-auto pr-1 space-y-2">

          {comments.fields.map((field, index) => (
            <div key={field.commentId} className="flex items-start gap-2">

              <ReactHookForm.Controller
                control={control}
                name={`perspective.nodes.${entityIndex}.comments.${index}.content`}
                render={({ field }) => (
                  <textarea
                    {...field}
                    className="flex-grow"
                    placeholder="コメントを入力..."
                  />
                )}
              />
              <Input.IconButton onClick={() => comments.remove(index)} icon={Icon.TrashIcon} hideText mini className="flex-none mt-1">
                コメント削除
              </Input.IconButton>
            </div>
          ))}

        </div>

        <Input.IconButton onClick={handleAddComment} icon={Icon.PlusIcon} className="mt-2 flex-none self-start">
          コメントを追加
        </Input.IconButton>
      </div>
    </div>
  );
};
