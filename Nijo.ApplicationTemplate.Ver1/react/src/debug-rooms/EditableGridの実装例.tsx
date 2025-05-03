import React, { useRef, useState, useCallback } from 'react';
import { EditableGrid } from '../layout/EditableGrid';
import type { EditableGridRef, EditableGridColumnDef } from '../layout/EditableGrid/index.d';
import { IconButton } from '../input/IconButton';

type ExampleUser = {
  id: number;
  name: string;
  age: number;
  email: string;
  active: boolean;
};

// サンプルデータ
const initialUsers: ExampleUser[] = Array.from({ length: 10 }, (_, i) => ({
  id: i + 1,
  name: `ユーザー${i + 1}`,
  age: 20 + Math.floor(Math.random() * 40),
  email: `user${i + 1}@example.com`,
  active: Math.random() > 0.3, // 30%の確率で非アクティブ
}));

/**
 * グリッドの実装例を確認するための画面。
 */
export const EditableGridの実装例: React.FC = () => {
  // グリッドへの参照
  const gridRef = useRef<EditableGridRef<ExampleUser>>(null);

  // データを状態として管理（行追加・削除のため）
  const [users, setUsers] = useState<ExampleUser[]>(initialUsers);

  // 最新のIDを追跡（新規行追加時に使用）
  const [nextId, setNextId] = useState(initialUsers.length + 1);

  // 行選択状態
  const [rowSelection, setRowSelection] = useState<Record<string, boolean>>({});

  // 列定義を作成
  const getColumnDefs = useCallback((cellType: any): EditableGridColumnDef<ExampleUser>[] => {
    return [
      {
        fieldPath: 'id',
        header: 'ID',
        defaultWidth: 80,
        // 読み取り専用の列
        isReadOnly: true,
      },
      {
        fieldPath: 'name',
        header: '名前',
        defaultWidth: 150,
        // 編集可能な列
        isReadOnly: false,
      },
      {
        fieldPath: 'age',
        header: '年齢',
        defaultWidth: 80,
        // 編集可能な列
        isReadOnly: false,
      },
      {
        fieldPath: 'email',
        header: 'メールアドレス',
        defaultWidth: 200,
        isReadOnly: false,
      },
      {
        fieldPath: 'active',
        header: 'アクティブ',
        defaultWidth: 100,
        // 読み取り専用の列
        isReadOnly: true,
      }
    ];
  }, []);

  // セル変更ハンドラ
  const handleCellChange = (rowIndex: number, fieldPath: string, value: any) => {
    console.log(`セル変更: 行=${rowIndex}, フィールド=${fieldPath}, 値=${value}`);

    // ユーザーデータを更新
    setUsers(prevUsers => {
      const newUsers = [...prevUsers];
      // @ts-ignore: 型の問題を一時的に無視
      newUsers[rowIndex][fieldPath] = value;
      return newUsers;
    });
  };

  // 選択行の表示ハンドラ
  const handleShowSelected = () => {
    if (!gridRef.current) return;

    const selectedRows = gridRef.current.getSelectedRows();
    if (selectedRows.length === 0) {
      alert('行が選択されていません');
      return;
    }

    alert(`選択された行: ${selectedRows.length}行\n${JSON.stringify(selectedRows.map(r => r.row), null, 2)}`);
  };

  // 行選択状態が変更された時の処理
  const handleRowSelectionChange = (updater: React.SetStateAction<Record<string, boolean>>) => {
    setRowSelection(updater);
  };

  // 行追加ハンドラ
  const handleAddRow = () => {
    const newUser: ExampleUser = {
      id: nextId,
      name: `新規ユーザー${nextId}`,
      age: 25,
      email: `newuser${nextId}@example.com`,
      active: true
    };

    setUsers(prevUsers => [...prevUsers, newUser]);
    setNextId(prevId => prevId + 1);
  };

  // 選択行削除ハンドラ
  const handleDeleteSelected = () => {
    if (!gridRef.current) return;

    const selectedRows = gridRef.current.getSelectedRows();
    if (selectedRows.length === 0) {
      alert('削除する行を選択してください');
      return;
    }

    // 選択された行のIDを取得
    const selectedIds = selectedRows.map(r => r.row.id);

    // 選択されていない行だけを残す
    setUsers(prevUsers => prevUsers.filter(user => !selectedIds.includes(user.id)));

    // 選択状態をクリア
    setRowSelection({});
  };

  return (
    <div className="p-4">
      <h1 className="text-xl font-bold mb-4">EditableGrid 実装例</h1>

      <div className="mb-4 flex space-x-2">
        <IconButton fill onClick={handleShowSelected}>
          選択行の表示
        </IconButton>
        <IconButton fill onClick={handleAddRow}>
          行を追加
        </IconButton>
        <IconButton outline onClick={handleDeleteSelected}>
          選択行を削除
        </IconButton>
      </div>

      <div className="h-[400px]">
        <EditableGrid<ExampleUser>
          ref={gridRef}
          rows={users}
          getColumnDefs={getColumnDefs}
          onChangeCell={handleCellChange}
          showCheckBox={true}
          className="h-full"
          rowSelection={rowSelection}
          onRowSelectionChange={handleRowSelectionChange}
        />
      </div>

      <div className="mt-6 p-4 border rounded bg-gray-50">
        <h2 className="text-lg font-semibold mb-2">説明</h2>
        <ul className="list-disc pl-5 space-y-1">
          <li><code>EditableGrid</code> は編集可能なグリッドコンポーネントです。</li>
          <li><code>rows</code> プロパティにデータの配列を指定します。</li>
          <li><code>getColumnDefs</code> 関数で列定義を提供します。各列には以下を設定できます：
            <ul className="list-disc pl-5 mt-1">
              <li><code>fieldPath</code>: データのプロパティパス</li>
              <li><code>header</code>: 列ヘッダーのテキスト</li>
              <li><code>defaultWidth</code>: 列の初期幅</li>
              <li><code>isReadOnly</code>: 編集可否</li>
            </ul>
          </li>
          <li><code>onChangeCell</code> イベントで編集されたセルの値を処理します。</li>
          <li><code>showCheckBox</code> で行選択チェックボックスの表示を制御します。</li>
          <li><code>rowSelection</code> と <code>onRowSelectionChange</code> で行選択状態を管理します。</li>
          <li><code>ref</code> を使用して以下のメソッドにアクセスできます：
            <ul className="list-disc pl-5 mt-1">
              <li><code>getSelectedRows()</code>: 選択された行を取得</li>
              <li><code>selectRow()</code>: プログラムで行を選択</li>
              <li><code>getActiveCell()</code>: アクティブなセルを取得</li>
              <li><code>getSelectedRange()</code>: 選択範囲を取得</li>
            </ul>
          </li>
          <li>マウスドラッグでセル範囲選択ができます。</li>
          <li>ダブルクリックでセル編集を開始します。</li>
          <li>「行を追加」ボタンで新しい行を追加できます。</li>
          <li>行を選択して「選択行を削除」ボタンをクリックすると、選択した行を削除できます。</li>
        </ul>
      </div>

      {/* デバッグ表示 */}
      <div className="mt-6 p-4 border rounded bg-gray-100">
        <h2 className="text-lg font-semibold mb-2">選択状態</h2>
        <pre className="text-xs overflow-auto max-h-32">
          {JSON.stringify(rowSelection, null, 2)}
        </pre>
      </div>
    </div>
  );
};
