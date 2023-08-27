import React from 'react';

export const NowLoading = ({ }: {
}) => {
  return (
    <div className="flex justify-center m-1" aria-label="読み込み中">
      <div className="animate-spin h-6 w-6 border-4 border-neutral-500 rounded-full border-t-transparent"></div>
    </div>
  )
}
