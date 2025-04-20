import { useState } from "react"

function App() {
  const [count, setCount] = useState(0)

  return (
    <div className="h-full bg-gray-100 flex flex-col justify-center">
      <h1 className="text-3xl font-bold text-gray-900 mb-5">Tailwind CSSが導入されました</h1>
      <p className="text-gray-600">美しいUIコンポーネントを簡単に作成できます</p>
    </div>
  )
}

export default App
