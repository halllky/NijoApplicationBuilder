import * as React from 'react';
import * as ReactDOM from 'react-dom';
import { useOutsideClick } from '../util';

export const ModalDialog = ({ open, onOutsideClick, children, className }: {
  open: boolean
  onOutsideClick?: () => void
  children?: React.ReactNode
  className?: string
}) => {

  const divRef = React.useRef<HTMLDivElement>(null);
  useOutsideClick(divRef, () => {
    onOutsideClick?.()
  }, [onOutsideClick])

  if (!open) return null;

  return ReactDOM.createPortal(
    <div className="fixed inset-0 z-10 flex items-center justify-center">

      {/* シェード */}
      <div className="absolute inset-0 bg-black/25" />

      {/* ダイアログ */}
      <div ref={divRef} className={className}>
        {children}
      </div>
    </div>,
    document.body,
  )
}