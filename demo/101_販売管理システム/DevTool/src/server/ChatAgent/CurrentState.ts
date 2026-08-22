import { CurrentStateDto } from "../../shared/devtool-api"

/**
 * エージェントの現在の状態を管理するクラス。
 * ブラウザリロードやサーバー再起動などをまたいで残したい永続化された情報を扱う。
 *
 * .nijo フォルダ配下にJSONファイルを置いてそのまま書き込む。
 * 永続化の詳細はクラス内に隠蔽する。
 * サーバーで1つであり、ユーザーごとに分けたりはしない。
 */
export class CurrentState {

  static readonly #FILE_NAME = "current-state.json"

  static load(): Promise<CurrentStateDto> {
    throw new Error('not implemented.')
  }

  static save(dto: CurrentStateDto): Promise<void> {
    throw new Error('not implemented.')
  }
}
