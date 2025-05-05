/**
 * TypeScript版MCPのTypeScriptファイル解析機能
 */

import * as fs from 'fs';
import * as path from 'path';
import * as ts from 'typescript';

export interface SymbolLocation {
  filePath: string;
  line: number;
  column: number;
}

export interface SymbolInfo {
  name: string;
  locations: SymbolLocation[];
}

/**
 * TypeScriptプロジェクトを解析するクラス
 */
export class TypeScriptFinder {
  private program: ts.Program;
  private typeChecker: ts.TypeChecker;
  private rootPath: string;

  constructor(projectPath: string) {
    this.rootPath = projectPath;

    // tsconfig.jsonの検索
    let tsconfigPath = '';
    if (fs.existsSync(path.join(projectPath, 'tsconfig.json'))) {
      tsconfigPath = path.join(projectPath, 'tsconfig.json');
    } else {
      // プロジェクトディレクトリを探索
      const findTsconfig = (dir: string): string => {
        const tsconfigPath = path.join(dir, 'tsconfig.json');
        if (fs.existsSync(tsconfigPath)) {
          return tsconfigPath;
        }

        const parentDir = path.dirname(dir);
        if (parentDir === dir) {
          return ''; // ルートディレクトリに到達
        }

        return findTsconfig(parentDir);
      };

      tsconfigPath = findTsconfig(projectPath);
    }

    if (!tsconfigPath) {
      throw new Error('tsconfig.jsonが見つかりません。');
    }

    // TypeScriptのプログラムを作成
    const configFile = ts.readConfigFile(tsconfigPath, ts.sys.readFile);
    const parsedCommandLine = ts.parseJsonConfigFileContent(
      configFile.config,
      ts.sys,
      path.dirname(tsconfigPath)
    );

    this.program = ts.createProgram({
      rootNames: parsedCommandLine.fileNames,
      options: parsedCommandLine.options
    });

    this.typeChecker = this.program.getTypeChecker();
  }

  /**
   * 指定された位置のシンボルの定義を検索
   */
  public findDefinition(sourceFilePath: string, line: number, character: number): SymbolInfo | null {
    try {
      const sourceFile = this.program.getSourceFile(sourceFilePath);
      if (!sourceFile) {
        throw new Error(`ソースファイルが見つかりません: ${sourceFilePath}`);
      }

      // ソースコード内の位置を計算
      const position = ts.getPositionOfLineAndCharacter(sourceFile, line - 1, character - 1);

      // 位置のノードを取得
      const node = this.findNodeAtPosition(sourceFile, position);
      if (!node) {
        throw new Error(`指定された位置にノードが見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      // シンボルを取得
      const symbol = this.getSymbolAtLocation(node);
      if (!symbol) {
        throw new Error(`シンボルが見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      // 定義位置を取得
      const declarations = symbol.declarations || [];
      if (declarations.length === 0) {
        return {
          name: symbol.name,
          locations: []
        };
      }

      const locations = declarations.map(declaration => {
        const sourceFile = declaration.getSourceFile();
        const { line, character } = ts.getLineAndCharacterOfPosition(sourceFile, declaration.getStart());
        return {
          filePath: sourceFile.fileName,
          line: line + 1,
          column: character + 1
        };
      });

      return {
        name: symbol.name,
        locations
      };
    } catch (error: any) {
      console.error(`定義の検索でエラーが発生しました: ${error.message}`);
      return null;
    }
  }

  /**
   * 指定された位置のシンボルの参照を検索
   */
  public findReferences(sourceFilePath: string, line: number, character: number): SymbolInfo | null {
    try {
      const sourceFile = this.program.getSourceFile(sourceFilePath);
      if (!sourceFile) {
        throw new Error(`ソースファイルが見つかりません: ${sourceFilePath}`);
      }

      // ソースコード内の位置を計算
      const position = ts.getPositionOfLineAndCharacter(sourceFile, line - 1, character - 1);

      // 位置のノードを取得
      const node = this.findNodeAtPosition(sourceFile, position);
      if (!node) {
        throw new Error(`指定された位置にノードが見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      // シンボルを取得
      const symbol = this.getSymbolAtLocation(node);
      if (!symbol) {
        throw new Error(`シンボルが見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      // 参照位置を検索
      const locations: SymbolLocation[] = [];

      for (const sourceFile of this.program.getSourceFiles()) {
        ts.forEachChild(sourceFile, function visit(node) {
          // ノードがシンボルへの参照かチェック
          if (ts.isIdentifier(node) && node.text === symbol.name) {
            const { line, character } = ts.getLineAndCharacterOfPosition(sourceFile, node.getStart());
            locations.push({
              filePath: sourceFile.fileName,
              line: line + 1,
              column: character + 1
            });
          }

          ts.forEachChild(node, visit);
        });
      }

      return {
        name: symbol.name,
        locations
      };
    } catch (error: any) {
      console.error(`参照の検索でエラーが発生しました: ${error.message}`);
      return null;
    }
  }

  /**
   * 指定されたクラスが実装する必要のあるメンバーを検索
   */
  public findRequiredMembers(sourceFilePath: string, line: number, character: number): string[] {
    try {
      const sourceFile = this.program.getSourceFile(sourceFilePath);
      if (!sourceFile) {
        throw new Error(`ソースファイルが見つかりません: ${sourceFilePath}`);
      }

      // ソースコード内の位置を計算
      const position = ts.getPositionOfLineAndCharacter(sourceFile, line - 1, character - 1);

      // 位置のノードを取得
      const node = this.findNodeAtPosition(sourceFile, position);
      if (!node || !ts.isClassDeclaration(node)) {
        throw new Error(`指定された位置にクラス宣言が見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      // クラスのシンボルを取得
      const classSymbol = this.typeChecker.getSymbolAtLocation(node.name!);
      if (!classSymbol) {
        throw new Error(`クラスシンボルが見つかりません: ${sourceFilePath}, ${line}, ${character}`);
      }

      const requiredMembers: string[] = [];

      // インターフェースを探索
      if (node.heritageClauses) {
        for (const clause of node.heritageClauses) {
          for (const type of clause.types) {
            const symbol = this.typeChecker.getSymbolAtLocation(type.expression);
            if (!symbol) continue;

            // インターフェースのメンバーを取得
            const members = this.typeChecker.getTypeAtLocation(type).getProperties();
            for (const member of members) {
              const name = member.getName();

              // 既に実装されているか確認
              const isImplemented = classSymbol.members?.has(ts.escapeLeadingUnderscores(name));

              if (!isImplemented) {
                requiredMembers.push(this.getFullSymbolDisplay(member));
              }
            }
          }
        }
      }

      return requiredMembers;
    } catch (error: any) {
      console.error(`必須メンバーの検索でエラーが発生しました: ${error.message}`);
      return [];
    }
  }

  /**
   * 指定された位置のノードを検索
   */
  private findNodeAtPosition(sourceFile: ts.SourceFile, position: number): ts.Node | undefined {
    function find(node: ts.Node): ts.Node | undefined {
      if (position >= node.getStart() && position <= node.getEnd()) {
        // 子ノードを探索
        for (const child of node.getChildren()) {
          const found = find(child);
          if (found) return found;
        }
        return node;
      }
      return undefined;
    }

    return find(sourceFile);
  }

  /**
   * 指定されたノードのシンボルを取得
   */
  private getSymbolAtLocation(node: ts.Node): ts.Symbol | undefined {
    if (ts.isIdentifier(node)) {
      return this.typeChecker.getSymbolAtLocation(node);
    }

    return undefined;
  }

  /**
   * シンボルの詳細表示を取得
   */
  private getFullSymbolDisplay(symbol: ts.Symbol): string {
    return this.typeChecker.symbolToString(
      symbol,
      undefined,
      ts.SymbolFlags.None,
      ts.SymbolFormatFlags.None
    );
  }
}
