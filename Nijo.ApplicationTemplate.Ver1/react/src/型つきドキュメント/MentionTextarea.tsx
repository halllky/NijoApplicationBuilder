import * as React from 'react';
import * as ReactRouter from 'react-router';
import * as ReactMention from 'react-mentions';
import { NijoUiOutletContextType } from '../スキーマ定義編集UI/types';
import { Entity, EntityAttribute, Perspective } from './types';
import useEvent from 'react-use-event-hook';
import { getNavigationUrl } from '../routes';

export type MentionTextareaProps = {
  value?: string
  onChange?: (value: string) => void
  isReadOnly?: boolean
  onKeyDown?: (event: React.KeyboardEvent<HTMLTextAreaElement>) => void
  placeholder?: string
  className?: string
}

/**
 * メンション付きのテキストエリア
 */
export const MentionTextarea = React.forwardRef((props: MentionTextareaProps, ref: React.Ref<HTMLTextAreaElement>) => {

  // データソース
  const { typedDoc } = ReactRouter.useOutletContext<NijoUiOutletContextType>()
  const getSuggestions: ReactMention.DataFunc = React.useCallback(async (query, callback) => {
    const sideMenuItems = await typedDoc.loadAppSettings()
    const entities: Entity[] = []
    for (const sideMenuItem of sideMenuItems.entityTypeList) {
      const perspective = await typedDoc.loadPerspectivePageData(sideMenuItem.entityTypeId)
      if (!perspective) continue;

      const filtered = perspective.perspective.nodes.filter(e => e.entityName.includes(query))
      entities.push(...filtered)
    }
    const suggestions = entities.map(e => ({
      id: e.entityId,
      display: e.entityName,
    }))
    callback(suggestions)
  }, [])

  const handleCommentChanged2: ReactMention.OnChangeHandlerFunc = useEvent(e => {
    props.onChange?.(e.target.value);
  })
  const handleKeyDownNewCommentText2: React.KeyboardEventHandler<HTMLTextAreaElement | HTMLInputElement> = useEvent(e => {
    props.onKeyDown?.(e as React.KeyboardEvent<HTMLTextAreaElement>);
  })

  // メンション部分をダブルクリックしたときの処理
  const navigate = ReactRouter.useNavigate()
  const handleDoubleClickMention = useEvent(async (part: MentionUtil.StringPart) => {
    if (!part.isMention) return;

    // typedDoc の中からリンク先エンティティのIDを含むページを探し
    // navigationでそこへ遷移する。
    const sideMenuItems = await typedDoc.loadAppSettings()

    for (const sideMenuItem of sideMenuItems.entityTypeList) {
      const perspective = await typedDoc.loadPerspectivePageData(sideMenuItem.entityTypeId)
      if (!perspective) continue;

      const filtered = perspective.perspective.nodes.filter(e => e.entityId === part.targetId)
      if (filtered.length === 0) continue;

      const url = getNavigationUrl({
        page: 'typed-document-perspective',
        perspectiveId: sideMenuItem.entityTypeId,
        focusEntityId: part.targetId,
      })
      navigate(url)
    }
  })

  // 読み取り専用の場合。
  // メンション部分をリンクに変換して表示する。
  if (props.isReadOnly) {
    return (
      <div className={`whitespace-pre-wrap break-all pb-[2px] ${props.className ?? ''}`}>
        {MentionUtil.parseAsMentionText(props.value).map((part, index) => (
          <React.Fragment key={index}>
            {part.isMention ? (
              <span
                className="text-sky-600 cursor-pointer hover:bg-sky-100"
                onDoubleClick={() => handleDoubleClickMention(part)}
              >
                @{part.text}
              </span>
            ) : (
              <span>
                {part.text}
              </span>
            )}
          </React.Fragment>
        ))}
        &nbsp;
      </div>
    )
  }

  // 編集可能な場合。
  // react mentions のメンションコンポーネントを使用する。
  return (
    <ReactMention.MentionsInput
      inputRef={ref}
      value={props.value ?? ''}
      onChange={handleCommentChanged2}
      onKeyDown={handleKeyDownNewCommentText2}
      spellCheck={false}
      placeholder={props.placeholder}
      className={`[&_textarea]:outline-none break-all resize-none field-sizing-content ${props.className ?? ''}`}
      suggestionsPortalHost={document.body}
      allowSuggestionsAboveCursor
      customSuggestionsContainer={children => (
        <div className="max-w-96 max-h-96 overflow-y-auto bg-white border border-gray-400">
          {children}
        </div>
      )}
    >
      <ReactMention.Mention
        trigger="@"
        markup="@[__display__](__id__)"
        data={getSuggestions}
        appendSpaceOnAdd
        displayTransform={(id, display) => `@${display}`}
        className="bg-sky-100 rounded-sm"
        renderSuggestion={(item, search, highlightedDisplay, index, focused) => (
          <span
            className={`inline-block w-full truncate text-sm select-none px-1 text-gray-800 ${focused ? 'bg-gray-200' : ''}`}
            title={item.display}
          >
            {item.display}
          </span>
        )}
      />
    </ReactMention.MentionsInput>
  )
})

export namespace MentionUtil {

  /**
   * メンション情報が含まれうる文字列を表示用の文字列に変換する。
   * 例えば元の文字列が `aaa \@[bbb](123) ccc \@[ddd](456) eee` の場合、
   * 戻り値は `aaa bbb ccc ddd eee` になる。
   */
  export const toPlainText = (text: string): string => {
    return parseAsMentionText(text)
      .map(part => part.isMention ? `@${part.text}` : part.text)
      .join('')
  }

  /**
   * このエンティティの中で登場するほかのエンティティへの参照を集める。
   */
  export const collectMentionIds = (entities: Entity[], attrDefs: EntityAttribute[]): {
    [sourceId: string]: {
      [targetId: string]: {
        /** ターゲットがこのページのエンティティかどうか */
        targetIsInThisPerspective: boolean
        /** メンション上の参照名。ターゲットがこのページのエンティティでない場合に使用 */
        mentionTexts: Set<string>
        /** 関係性 */
        relations: Set<string>
      }
    }
  } => {

    const attrDefMap = new Map(attrDefs.map(x => [x.attributeId, x]))
    const thisPerspectiveEntityIds = new Set(entities.map(x => x.entityId))
    const result: {
      [sourceId: string]: {
        [targetId: string]: {
          targetIsInThisPerspective: boolean
          mentionTexts: Set<string>
          relations: Set<string>
        }
      }
    } = {}

    for (const node of entities) {
      // エンティティ名
      for (const part of parseAsMentionText(node.entityName)) {
        if (!part.isMention) continue;

        const objSource = result[node.entityId] ?? (result[node.entityId] = {})
        const objTarget = objSource[part.targetId] ?? (objSource[part.targetId] = {
          targetIsInThisPerspective: thisPerspectiveEntityIds.has(part.targetId),
          mentionTexts: new Set(),
          relations: new Set(),
        })
        objTarget.mentionTexts.add(part.text)
      }

      // 属性
      for (const [attrId, attrValue] of Object.entries(node.attributeValues)) {
        const attrDef = attrDefMap.get(attrId)
        if (!attrDef) continue;

        const parts = parseAsMentionText(attrValue)
        for (const part of parts) {
          if (!part.isMention) continue;

          const objSource = result[node.entityId] ?? (result[node.entityId] = {})
          const objTarget = objSource[part.targetId] ?? (objSource[part.targetId] = {
            targetIsInThisPerspective: thisPerspectiveEntityIds.has(part.targetId),
            mentionTexts: new Set(),
            relations: new Set(),
          })
          objTarget.mentionTexts.add(part.text)
          objTarget.relations.add(attrDef.attributeName)
        }
      }

      // コメント
      for (const comment of node.comments) {
        for (const part of parseAsMentionText(comment.content)) {
          if (!part.isMention) continue;

          const objSource = result[node.entityId] ?? (result[node.entityId] = {})
          const objTarget = objSource[part.targetId] ?? (objSource[part.targetId] = {
            targetIsInThisPerspective: thisPerspectiveEntityIds.has(part.targetId),
            mentionTexts: new Set(),
            relations: new Set(),
          })
          objTarget.mentionTexts.add(part.text)
          objTarget.relations.add('コメント')
        }
      }
    }

    return result
  }

  /**
   * メンション情報つきテキストとして解釈する。
   * 例えば元の文字列が `aaa \@[bbb](123) ccc \@[ddd](456) eee` の場合、
   * 戻り値は以下になる。
   *
   * ```javascript
   * [
   *   { isMention: false, text: 'aaa ' },
   *   { isMention: true, text: 'bbb', targetId: '123' },
   *   { isMention: false, text: ' ccc ' },
   *   { isMention: true, text: 'ddd', targetId: '456' },
   *   { isMention: false, text: ' eee' }
   * ]
   * ```
   */
  export const parseAsMentionText = (text: string | undefined): StringPart[] => {
    if (!text) return []

    const result: StringPart[] = []
    const regex = /@\[(.*?)\]\((.*?)\)/g
    let lastIndex = 0
    let match

    while ((match = regex.exec(text)) !== null) {
      // 前回のマッチから今回のマッチまでの非メンション部分を追加
      if (match.index > lastIndex) {
        result.push({
          isMention: false,
          text: text.substring(lastIndex, match.index),
        })
      }

      // メンション部分を追加
      const [_, display, id] = match
      result.push({
        isMention: true,
        text: display,
        targetId: id,
      })

      lastIndex = regex.lastIndex
    }

    // 最後のマッチ以降の非メンション部分を追加
    if (lastIndex < text.length) {
      result.push({
        isMention: false,
        text: text.substring(lastIndex),
      })
    }

    return result
  }

  /** 文字列の一部分 */
  export type StringPart
    = { isMention: false, text: string }
    | { isMention: true, text: string, targetId: string }
}
