# 文字コードをUTF-8に設定
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8

Write-Host "===================================================="
Write-Host "Nijo.ApplicationTemplate.Ver1 実行ツール（Cursor Agent用）"
Write-Host "===================================================="
Write-Host ""

# Nijo.exe をビルド
dotnet build Nijo/Nijo.csproj

# エラーがあったら処理中断
if ($LASTEXITCODE -ne 0) {
    Write-Host "ビルドに失敗しました。"
    exit 1
}

# Ver1プロジェクトに対して nijo run コマンドを実行
./Nijo/bin/Debug/net9.0/nijo.exe "run" "Nijo.ApplicationTemplate.Ver1"
