import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App, { IS_EMBEDDED } from './App.tsx'

// CSSの読み込み
import './App.css'
if (IS_EMBEDDED()) {
  import('./App.NijoUi.css')
}

// AllotmentのCSS
import 'allotment/dist/style.css'

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
