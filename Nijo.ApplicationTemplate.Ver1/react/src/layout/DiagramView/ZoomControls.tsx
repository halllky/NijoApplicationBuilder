import * as Icon from "@heroicons/react/24/outline"
import * as Input from "../../input"
import { ZoomProps } from "./types"
import React from "react"

/**
 * ズーム操作のUIコントロール
 */
export default function ZoomControls({
  zoom,
  onZoomIn,
  onZoomOut,
  onResetZoom
}: ZoomProps) {
  return (
    <div className="flex flex-col">
      <div className="text-sm select-none">
        ズーム {Math.round(zoom * 100)}%
      </div>
      <div className="flex gap-1">
        <Input.IconButton icon={Icon.MinusIcon} onClick={onZoomOut} fill hideText>
          ズームアウト
        </Input.IconButton>
        <Input.IconButton icon={Icon.PlusIcon} onClick={onZoomIn} fill hideText>
          ズームイン
        </Input.IconButton>
        <Input.IconButton icon={Icon.ArrowPathIcon} onClick={onResetZoom} fill hideText>
          リセット
        </Input.IconButton>
      </div>
    </div>
  )
}
