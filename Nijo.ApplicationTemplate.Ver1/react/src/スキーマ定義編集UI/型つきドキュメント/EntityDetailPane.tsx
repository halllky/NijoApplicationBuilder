import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import { Entity, EntityAttribute, EntityAttributeValues, Perspective, PerspectivePageData, TypedDocumentComment } from './types'; // 型をインポート
import * as Input from '../../input'; // Inputコンポーネントをインポート
import * as Icon from '@heroicons/react/24/solid'; // アイコンをインポート
import { UUID } from 'uuidjs'; // UUID生成のため
import useEvent from 'react-use-event-hook';
import dayjs from 'dayjs';

// Propsの型定義
interface EntityDetailPaneProps {
  entity: Entity
  onEntityChanged: (entity: Entity) => void
  perspective: Perspective
  entityIndex: number;
}

/**
 * エンティティ1件の詳細欄
 */
export const EntityDetailPane: React.FC<EntityDetailPaneProps> = ({
  entity,
  onEntityChanged,
  perspective,
  entityIndex,
}) => {

  const handleAttributeChanged = useEvent((attribute: EntityAttribute, value: string) => {
    const attributeValues: EntityAttributeValues = { ...entity.attributeValues };
    if (value) {
      attributeValues[attribute.attributeId] = value;
    } else {
      delete attributeValues[attribute.attributeId];
    }
    onEntityChanged({ ...entity, attributeValues });
  })

  // ---------------------------------------

  // 新規コメント追加
  const [newCommentText, setNewCommentText] = React.useState('');
  const handleChangeNewCommentText: React.ChangeEventHandler<HTMLTextAreaElement> = useEvent((e) => {
    setNewCommentText(e.target.value);
  })
  const handleKeyDownNewCommentText: React.KeyboardEventHandler<HTMLTextAreaElement> = useEvent((e) => {
    if (e.ctrlKey && e.key === 'Enter') {
      handleAddComment();
    }
  })
  const handleAddComment = useEvent(() => {
    if (!newCommentText) return;
    const newComment: TypedDocumentComment = {
      commentId: UUID.generate(), // 新しいコメントIDを生成
      content: newCommentText,
      author: "current_user", // TODO: 実際のユーザー情報に置き換える
      createdAt: new Date().toISOString(),
    };
    onEntityChanged({
      ...entity,
      comments: [...entity.comments, newComment],
    });
    setNewCommentText('');
  })

  // 既存コメントの編集・削除
  const handleCommentChanged = useEvent((comment: TypedDocumentComment) => {
    onEntityChanged({
      ...entity,
      comments: entity.comments.map((c) => c.commentId === comment.commentId ? comment : c),
    });
  })
  const handleCommentDeleted = useEvent((comment: TypedDocumentComment) => {
    onEntityChanged({
      ...entity,
      comments: entity.comments.filter((c) => c.commentId !== comment.commentId),
    });
  })

  return (
    <div className="px-1 h-full flex flex-col gap-2">

      {/* エンティティ名 */}
      <textarea
        value={entity.entityName}
        onChange={(e) => onEntityChanged({ ...entity, entityName: e.target.value })}
        className="w-full text-md font-bold outline-none resize-none field-sizing-content"
      />

      <div className="flex-1 flex flex-col justify-start gap-2 py-1 overflow-y-auto">

        {/* 属性 */}
        <div className="flex flex-col gap-1">
          {perspective.attributes.map((attribute) => (
            <AttributeValueView
              key={attribute.attributeId}
              perspective={perspective}
              attribute={attribute}
              value={entity.attributeValues[attribute.attributeId]}
              onChange={handleAttributeChanged}
            />
          ))}
        </div>

        <div className="flex-1"></div>

        {/* コメント */}
        <div className="flex flex-col gap-1">
          <span className="text-xs select-none text-gray-500">
            コメント
          </span>

          {entity.comments.map((comment, index) => (
            <CommentView
              key={comment.commentId}
              comment={comment}
              onCommentChanged={handleCommentChanged}
              onCommentDeleted={handleCommentDeleted}
              index={index}
            />
          ))}
        </div>
      </div>

      {/* コメントを追加 */}
      <div className="flex flex-col items-start px-1 py-px gap-px border border-gray-500 overflow-x-hidden">
        <textarea
          value={newCommentText}
          onChange={handleChangeNewCommentText}
          onKeyDown={handleKeyDownNewCommentText}
          placeholder="新規コメント"
          className="text-md outline-none self-stretch resize-none field-sizing-content"
        />
        <Input.IconButton onClick={handleAddComment} className="mt-2 flex-none self-start">
          コメントを追加（Ctrl + Enter）
        </Input.IconButton>
      </div>
    </div>
  );
};

/**
 * 属性名と値のペア1件分の表示
 */
const AttributeValueView = ({ perspective, attribute, value, onChange }: {
  perspective: Perspective
  attribute: EntityAttribute
  value: string
  onChange: (attribute: EntityAttribute, value: string) => void
}) => {

  const handleChangeTextarea = useEvent((e: React.ChangeEvent<HTMLTextAreaElement>) => {
    onChange(attribute, e.target.value);
  })

  if (attribute.attributeType === 'word') {
    // 単語型の属性
    return (
      <label className="self-stretch flex items-start gap-1 px-px border border-gray-300">
        <div className="flex-none flex items-center" style={{ width: perspective.detailPageLabelWidth ?? DEFAULT_LABEL_WIDTH }}>
          <span className="text-xs select-none text-gray-500">
            {attribute.attributeName}
          </span>
          &nbsp;
        </div>

        <textarea
          value={value}
          onChange={handleChangeTextarea}
          spellCheck={false}
          className="w-full px-px outline-none resize-none field-sizing-content"
        />
      </label>
    )
  } else {
    // 複数行テキストの属性
    return (
      <label className="self-stretch flex flex-col items-start gap-1 px-px border border-gray-300">
        <span className="text-xs select-none text-gray-500">
          {attribute.attributeName}
        </span>
        <textarea
          value={value}
          onChange={handleChangeTextarea}
          spellCheck={false}
          className="self-stretch outline-none resize-none field-sizing-content"
        />
      </label>
    )
  }
}

const DEFAULT_LABEL_WIDTH = '10rem';

// ---------------------------------------

/**
 * コメント1件の表示
 */
const CommentView = ({ comment, onCommentChanged, onCommentDeleted }: {
  comment: TypedDocumentComment
  onCommentChanged: (comment: TypedDocumentComment) => void
  onCommentDeleted: (comment: TypedDocumentComment) => void
  index: number
}) => {
  // 編集モード
  const [isEditing, setIsEditing] = React.useState(false);
  const [unCommitedText, setUnCommitedText] = React.useState(comment.content);

  const handleStartEditing = useEvent(() => {
    setUnCommitedText(comment.content);
    setIsEditing(true);
  })
  const handleChangeText: React.ChangeEventHandler<HTMLTextAreaElement> = useEvent((e) => {
    setUnCommitedText(e.target.value);
  })
  const handleSave = useEvent(() => {
    onCommentChanged({ ...comment, content: unCommitedText });
    setIsEditing(false);
  })
  const handleCancel = useEvent(() => {
    if (unCommitedText !== comment.content && !window.confirm('キャンセルしますか？')) return;
    setIsEditing(false);
  })

  // コメント削除
  const handleDelete = useEvent(() => {
    if (comment.content && !window.confirm('コメントを削除しますか？')) return;
    onCommentDeleted(comment);
  })

  return (
    <div className={`flex flex-col px-1 ${isEditing ? 'border border-gray-300' : ''}`}>
      <div className="flex items-start gap-2">

        {isEditing ? (
          <textarea
            value={unCommitedText}
            onChange={handleChangeText}
            className="flex-grow outline-none resize-none field-sizing-content"
            placeholder="コメントを入力..."
          />
        ) : (
          <div className="flex-grow">
            {comment.content}
          </div>
        )}

        {isEditing ? (
          <>
            <Input.IconButton onClick={handleCancel} icon={Icon.XMarkIcon} hideText mini className="flex-none mt-1">
              キャンセル
            </Input.IconButton>
            <Input.IconButton onClick={handleSave} icon={Icon.CheckIcon} hideText mini className="flex-none mt-1">
              保存
            </Input.IconButton>
          </>
        ) : (
          <>
            <Input.IconButton onClick={handleStartEditing} icon={Icon.PencilIcon} hideText mini className="flex-none mt-1">
              編集
            </Input.IconButton>
            <Input.IconButton onClick={handleDelete} icon={Icon.TrashIcon} hideText mini className="flex-none mt-1">
              コメント削除
            </Input.IconButton>
          </>
        )}
      </div>

      <div className="flex justify-end items-center gap-2">
        <span className="text-xs text-gray-500 select-none">
          {dayjs(comment.createdAt).format('YYYY-MM-DD HH:mm')}
        </span>
        <span className="text-xs text-gray-500 select-none">
          {comment.author}
        </span>
      </div>
    </div>
  )
}
