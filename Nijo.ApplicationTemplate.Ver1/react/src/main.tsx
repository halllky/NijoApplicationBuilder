import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import App from './App.tsx'

// CSSの読み込み
import './App.css'
if (import.meta.env.mode === 'nijo-ui') {
  import('./App.NijoUi.css')
} else {
  import('./App.ReflectionPage.css')
}

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App />
  </StrictMode>,
)
