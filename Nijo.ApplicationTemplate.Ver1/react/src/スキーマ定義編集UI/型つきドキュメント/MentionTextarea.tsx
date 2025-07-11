import * as React from 'react';
import * as ReactRouter from 'react-router';
import * as ReactMention from 'react-mentions';
import { NijoUiOutletContextType } from '../types';
import { Entity, EntityAttribute, Perspective } from './types';
import useEvent from 'react-use-event-hook';
import { getNavigationUrl } from '../../routes';
import { MentionInputWrapper, ReadOnlyMentionText, MentionUtil } from '../UI';

export type MentionTextareaProps = {
  value?: string
  onChange?: (value: string) => void
  isReadOnly?: boolean
  onKeyDown?: (event: React.KeyboardEvent<HTMLTextAreaElement>) => void
  placeholder?: string
  className?: string
}

/**
 * 読み取り専用のメンション表示コンポーネント
 */
const MentionReadOnlyRenderer: React.FC<{ value?: string; className?: string }> = ({ value, className }) => {
  const { typedDoc: { appSettings, loadPerspectivePageData } } = ReactRouter.useOutletContext<NijoUiOutletContextType>()
  const navigate = ReactRouter.useNavigate()

  // メンション部分をクリックしたときの処理
  const handleClickMention = useEvent(async (part: MentionUtil.StringPart) => {
    if (!part.isMention) return;

    // typedDoc の中からリンク先エンティティのIDを含むページを探し
    // navigationでそこへ遷移する。
    for (const sideMenuItem of appSettings.entityTypeList) {
      const perspective = await loadPerspectivePageData(sideMenuItem.entityTypeId)
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

  return (
    <ReadOnlyMentionText className={`whitespace-pre-wrap break-all pb-[2px] ${className ?? ''}`} onClickMention={handleClickMention}>
      {value}
    </ReadOnlyMentionText>
  )
}

/**
 * メンション付きのテキストエリア
 */
export const MentionTextarea = React.forwardRef((props: MentionTextareaProps, ref: React.Ref<HTMLTextAreaElement>) => {

  // データソース
  const { typedDoc: { appSettings, loadPerspectivePageData } } = ReactRouter.useOutletContext<NijoUiOutletContextType>()
  const getSuggestions: ReactMention.DataFunc = React.useCallback(async (query, callback) => {
    const entities: Entity[] = []
    for (const sideMenuItem of appSettings.entityTypeList) {
      const perspective = await loadPerspectivePageData(sideMenuItem.entityTypeId)
      if (!perspective) continue;

      const filtered = perspective.perspective.nodes.filter(e => e.entityName.includes(query))
      entities.push(...filtered)
    }
    const suggestions = entities.map(e => ({
      id: e.entityId,
      display: e.entityName,
    }))
    callback(suggestions)
  }, [appSettings, loadPerspectivePageData])

  return (
    <MentionInputWrapper
      ref={ref}
      value={props.value}
      onChange={props.onChange}
      onKeyDown={props.onKeyDown}
      placeholder={props.placeholder}
      className={props.className}
      isReadOnly={props.isReadOnly}
      getSuggestions={getSuggestions}
      readOnlyRenderer={MentionReadOnlyRenderer}
    />
  )
})
