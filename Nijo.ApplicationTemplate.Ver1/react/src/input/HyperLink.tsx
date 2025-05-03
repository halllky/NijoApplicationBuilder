import * as ReactRouter from "react-router-dom"

type HyperLinkProps = {
  children?: React.ReactNode
  className?: string
} & (
    | { to: string, href?: never }
    | { to?: never, href: string }
  )

/**
 * ハイパーリンク。
 * toが設定されている場合はこのアプリケーション内部の画面への、
 * hrefが設定されている場合は外部の画面へのリンクを表示する。
 */
export const HyperLink = (props: HyperLinkProps) => {
  return props.to ? (
    <ReactRouter.NavLink to={props.to} className={`text-sky-600 underline ${props.className ?? ''}`}>
      {props.children}
    </ReactRouter.NavLink>
  ) : (
    <a href={props.href} className={`text-sky-600 underline ${props.className ?? ''}`}>
      {props.children}
    </a>
  )
}