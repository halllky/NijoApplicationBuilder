import React from 'react';
import * as ReactMention from 'react-mentions';
import useEvent from 'react-use-event-hook';
import LinkifyJs from 'linkifyjs';
import { Entity, EntityAttribute } from '../型つきドキュメント/types';

export type MentionInputWrapperProps = {
  value?: string
  onChange?: (value: string) => void
  onKeyDown?: (event: React.KeyboardEvent<HTMLTextAreaElement>) => void
  placeholder?: string
  className?: string
  isReadOnly?: boolean
  /** メンション候補データを取得する関数 */
  getSuggestions: ReactMention.DataFunc
  /** 読み取り専用時の表示用コンポーネント（オプション） */
  readOnlyRenderer?: React.ComponentType<{ value?: string; className?: string }>
}

/**
 * メンション機能付きテキストエリアの共通コンポーネント
 */
export const MentionInputWrapper = React.forwardRef<HTMLTextAreaElement, MentionInputWrapperProps>(({
  value,
  onChange,
  onKeyDown,
  placeholder,
  className,
  isReadOnly,
  getSuggestions,
  readOnlyRenderer: ReadOnlyRenderer,
}, ref) => {

  const handleChanged: ReactMention.OnChangeHandlerFunc = useEvent(e => {
    onChange?.(e.target.value);
  })

  const handleKeyDown: React.KeyboardEventHandler<HTMLTextAreaElement | HTMLInputElement> = useEvent(e => {
    onKeyDown?.(e as React.KeyboardEvent<HTMLTextAreaElement>);
  })

  // 読み取り専用の場合
  if (isReadOnly) {
    if (ReadOnlyRenderer) {
      return <ReadOnlyRenderer value={value} className={className} />
    }
    // デフォルトの読み取り専用表示
    return (
      <div className={`whitespace-pre-wrap break-all pb-[2px] ${className ?? ''}`}>
        {MentionUtil.parseAsMentionText(value).map((part, index) => (
          <React.Fragment key={index}>
            {part.isMention ? (
              <span className="text-sky-600 bg-sky-100 rounded-sm px-1">
                @{part.text}
              </span>
            ) : (
              <span>{part.text}</span>
            )}
          </React.Fragment>
        ))}
        &nbsp;
      </div>
    )
  }

  // 編集可能な場合
  return (
    <ReactMention.MentionsInput
      inputRef={ref}
      value={value ?? ''}
      onChange={handleChanged}
      onKeyDown={handleKeyDown}
      spellCheck={false}
      placeholder={placeholder}
      className={`[&_textarea]:outline-none break-all resize-none field-sizing-content ${className ?? ''}`}
      suggestionsPortalHost={document.body}
      allowSuggestionsAboveCursor
      style={MentionUtil.STYLE_MENTION_SUGGESTIONS}
    >
      <ReactMention.Mention
        trigger="@"
        markup="@[__display__](__id__)"
        data={getSuggestions}
        displayTransform={(id, display) => `@${display}`}
        className="bg-sky-100 rounded-sm"
        renderSuggestion={(item, search, highlightedDisplay, index, focused) => (
          <span
            className={`inline-block w-full truncate text-sm select-none px-1 text-gray-800 ${focused ? 'bg-gray-200' : ''}`}
            title={item.display}
          >
            {item.display}
            &nbsp;
          </span>
        )}
      />
    </ReactMention.MentionsInput>
  )
})

export const LINKIFY_OPTIONS: LinkifyJs.Opts = {
  target: '_blank',
  className: 'text-sky-600 cursor-pointer underline underline-offset-2 hover:bg-sky-100',
}

export namespace MentionUtil {

  export const STYLE_MENTION_SUGGESTIONS: ReactMention.MentionsInputProps['style'] = {
    suggestions: {
      list: {
        backgroundColor: 'white',
        maxHeight: '200px',
        overflowY: 'auto',
        border: '1px solid rgba(0,0,0)',
      },
    },
  }

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
