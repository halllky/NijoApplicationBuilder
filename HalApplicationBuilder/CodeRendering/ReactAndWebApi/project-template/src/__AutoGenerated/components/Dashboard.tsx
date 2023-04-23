import React, { useCallback } from 'react';
import { IconButton } from './IconButton';

export const Dashboard = () => {
    const recreateDatabase = useCallback(() => {
        if (window.confirm('DBを再作成します。データは全て削除されます。よろしいですか？')) {
            fetch(`https://localhost:7275/HalappDebug/recreate-database`, {
                method: 'PUT',
            })
        }
    }, [])
    return (
        <div>
            top page
            <IconButton onClick={recreateDatabase}>DB再作成</IconButton>
        </div>
    )
}
