# Instrallation
```
npm ci
```

# Debug
```
npm run tauri dev
```

ファイル指定でデバッグする場合
```
npm run tauri dev -- -- -- ../TESTFILES/my-test-data.json
```
※ first  -- = for npm
※ second -- = for runner
※ third  -- = for application

# Release
```
npm run tauri build
```
