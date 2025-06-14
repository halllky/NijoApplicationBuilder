import * as React from 'react';
import * as ReactHookForm from 'react-hook-form';
import { Entity, EntityAttribute, EntityAttributeValues, Perspective, PerspectivePageData, TypedDocumentComment } from './types'; // 型をインポート
import * as Input from '../input'; // Inputコンポーネントをインポート
import * as Icon from '@heroicons/react/24/solid'; // アイコンをインポート
import { UUID } from 'uuidjs'; // UUID生成のため
import useEvent from 'react-use-event-hook';
import dayjs from 'dayjs';
import { MentionTextarea } from './MentionTextarea';

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

  const [isEditing, setIsEditing] = React.useState(false);
  const entityNameRef = React.useRef<HTMLTextAreaElement>(null);
  const handleStartEditing = useEvent(() => {
    setIsEditing(true);
    setTimeout(() => {
      entityNameRef.current?.focus();
      entityNameRef.current?.setSelectionRange(entity.entityName.length, entity.entityName.length);
    }, 0);
  })
  const handleEndEditing = useEvent(() => {
    setIsEditing(false);
  })

  // ---------------------------------------
  // エンティティ名
  const handleEntityNameChanged = useEvent((entityName: string) => {
    onEntityChanged({ ...entity, entityName });
  })

  // ---------------------------------------
  // 属性
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

  // ---------------------------------------
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
    <div className="px-1 pb-1 h-full flex flex-col gap-2">

      <div className="flex flex-row-reverse items-start gap-1">
        {isEditing ? (
          <Input.IconButton onClick={handleEndEditing} icon={Icon.CheckIcon} hideText mini className="mt-1">
            編集終了
          </Input.IconButton>
        ) : (
          <Input.IconButton onClick={handleStartEditing} icon={Icon.PencilIcon} hideText mini className="mt-1">
            編集
          </Input.IconButton>
        )}

        {/* エンティティ名 */}
        <EntityNameView
          ref={entityNameRef}
          entityName={entity.entityName}
          onChange={handleEntityNameChanged}
          onCtrlEnter={handleEndEditing}
          isEditing={isEditing}
          className="flex-1"
        />
      </div>

      <div className="flex-1 flex flex-col justify-start gap-2 py-1 overflow-y-auto">

        {/* 属性 */}
        <div className="flex flex-col gap-1">
          {perspective.attributes.filter(a => !a.invisibleInDetail).map((attribute, index) => (
            <React.Fragment key={attribute.attributeId}>
              {index > 0 && (
                <hr className="border-gray-200" />
              )}
              <AttributeValueView
                perspective={perspective}
                attribute={attribute}
                value={entity.attributeValues[attribute.attributeId]}
                onChange={handleAttributeChanged}
                onCtrlEnter={handleEndEditing}
                isEditing={isEditing}
              />
            </React.Fragment>
          ))}
        </div>

        <div className="flex-1"></div>

        {/* コメント */}
        <hr className="border-gray-200" />
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
        <MentionTextarea
          value={newCommentText}
          onChange={setNewCommentText}
          onKeyDown={handleKeyDownNewCommentText}
          placeholder="新規コメント"
          className="self-stretch"
        />
        <Input.IconButton onClick={handleAddComment} className="mt-2 flex-none self-start">
          コメントを追加（Ctrl + Enter）
        </Input.IconButton>
      </div>
    </div>
  );
};

/**
 * エンティティ名の表示
 */
const EntityNameView = React.forwardRef<HTMLTextAreaElement, {
  entityName: string
  onChange: (entityName: string) => void
  isEditing: boolean
  onCtrlEnter: () => void
  className?: string
}>(({ entityName, onChange, isEditing, onCtrlEnter, className }, ref) => {

  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey && e.key === 'Enter') onCtrlEnter();
    if (e.key === 'Escape') onCtrlEnter();
  })

  return (
    <MentionTextarea
      ref={ref}
      value={entityName}
      onChange={onChange}
      onKeyDown={handleKeyDown}
      className={`font-bold border ${isEditing ? 'border-gray-500' : 'border-transparent select-all'} ${className ?? ''}`}
      isReadOnly={!isEditing}
    />
  )
})

/**
 * 属性名と値のペア1件分の表示
 */
const AttributeValueView = ({ perspective, attribute, value, onChange, isEditing, onCtrlEnter }: {
  perspective: Perspective
  attribute: EntityAttribute
  value: string
  onChange: (attribute: EntityAttribute, value: string) => void
  isEditing: boolean
  onCtrlEnter: () => void
}) => {

  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey && e.key === 'Enter') onCtrlEnter();
    if (e.key === 'Escape') onCtrlEnter();
  })
  const handleChange = useEvent((value: string) => {
    onChange(attribute, value);
  })
  const handleChangeSelect = useEvent((e: React.ChangeEvent<HTMLSelectElement>) => {
    onChange(attribute, e.target.value);
  })

  if (attribute.attributeType === 'word') {
    // 単語型の属性
    return (
      <div className="self-stretch flex items-start gap-1">
        <div className="flex-none flex items-center" style={{ width: perspective.detailPageLabelWidth ?? DEFAULT_LABEL_WIDTH }}>
          <span className="text-xs select-none text-gray-500">
            {attribute.attributeName}
          </span>
          <span className="select-none">
            &nbsp;
          </span>
        </div>

        <MentionTextarea
          value={value}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          isReadOnly={!isEditing}
          className={`w-full border ${isEditing ? 'border-gray-500' : 'border-transparent select-all'}`}
        />
      </div>
    )
  } else if (attribute.attributeType === 'description') {
    // 複数行テキストの属性
    return (
      <div className="self-stretch flex flex-col items-start gap-1">
        <div className="self-stretch flex flex-wrap items-center">
          <span className="text-xs select-none text-gray-500">
            {attribute.attributeName}
          </span>
          <div className="flex-1"></div>
        </div>
        <MentionTextarea
          value={value}
          onChange={handleChange}
          onKeyDown={handleKeyDown}
          isReadOnly={!isEditing}
          className={`self-stretch border ${isEditing ? 'border-gray-500' : 'border-transparent'}`}
        />
      </div>
    )

  } else if (attribute.attributeType === 'select') {
    // 選択肢の属性
    const options = perspective.attributes.find(a => a.attributeId === attribute.attributeId)?.selectOptions ?? [];

    return (
      <div className="self-stretch flex items-start gap-1">
        <div className="flex-none flex items-center" style={{ width: perspective.detailPageLabelWidth ?? DEFAULT_LABEL_WIDTH }}>
          <span className="text-xs select-none text-gray-500">
            {attribute.attributeName}
          </span>
          <span className="select-none">
            &nbsp;
          </span>
        </div>
        <div className="flex-1">
          <select
            value={value}
            onChange={handleChangeSelect}
            className={`w-full border ${isEditing ? 'border-gray-500' : 'border-transparent select-all'}`}
            disabled={!isEditing}
          >
            <option value=""></option>
            {options.map((option) => (
              <option key={option} value={option}>
                {option}
              </option>
            ))}
          </select>
        </div>
      </div>
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
  const [unCommitedText, setUnCommitedText] = React.useState<string>();
  const textareaRef = React.useRef<HTMLTextAreaElement>(null);
  const handleStartEditing = useEvent(() => {
    setUnCommitedText(comment.content);
    setIsEditing(true);
    setTimeout(() => {
      textareaRef.current?.focus();
      textareaRef.current?.setSelectionRange(comment.content.length, comment.content.length);
    }, 0);
  })
  const handleSave = useEvent(() => {
    if (unCommitedText === undefined) return;
    onCommentChanged({ ...comment, content: unCommitedText });
    setIsEditing(false);
    setUnCommitedText(undefined);
  })
  const handleCancel = useEvent(() => {
    if (unCommitedText !== comment.content && !window.confirm('キャンセルしますか？')) return;
    setIsEditing(false);
    setUnCommitedText(undefined);
  })
  const handleKeyDown = useEvent((e: React.KeyboardEvent<HTMLTextAreaElement>) => {
    if (e.ctrlKey && e.key === 'Enter') handleSave();
    if (e.key === 'Escape') handleCancel();
  })

  // コメント削除
  const handleDelete = useEvent(() => {
    if (comment.content && !window.confirm('コメントを削除しますか？')) return;
    onCommentDeleted(comment);
  })

  return (
    <div className={`flex flex-col border ${isEditing ? 'border-gray-500' : 'border-transparent'}`}>
      <div className="flex items-start gap-2">
        <MentionTextarea
          ref={textareaRef}
          value={unCommitedText ?? comment.content}
          onChange={setUnCommitedText}
          onKeyDown={handleKeyDown}
          isReadOnly={!isEditing}
          placeholder="コメントを入力..."
          className="flex-grow"
        />

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
