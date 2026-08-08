import { callAspNetCoreApiAsync } from './callAspNetCoreApiAsync'

// 自動生成されたエンドポイントを呼び出す例
export default function example() {
  const apiButton = document.querySelector<HTMLButtonElement>('#api-button')!
  const apiResponse = document.querySelector<HTMLDivElement>('#api-response')!

  apiButton.addEventListener('click', async () => {
    try {
      apiResponse.textContent = '読み込み中...'
      const response = await callAspNetCoreApiAsync('/api/example', {
        method: 'GET'
      })

      if (response.ok) {
        apiResponse.textContent = await response.text()
      } else {
        apiResponse.textContent = `エラー: ${response.status} ${response.statusText}`
      }
    } catch (error) {
      apiResponse.textContent = `エラー: ${error}`
    }
  })

  return `
    <div class="card">
      <button id="api-button" type="button">ASP.NET Core API 呼び出し</button>
      <div id="api-response" style="white-space: pre-wrap; text-align: left;"></div>
    </div>
    `
}
