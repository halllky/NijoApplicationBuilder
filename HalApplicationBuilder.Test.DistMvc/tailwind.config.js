const colors = require('tailwindcss/colors');

/** @type {import('tailwindcss').Config} */
module.exports = {
  purge: {
    enabled: true,
    content: [
      './**/*.{razor,cshtml,html}'
    ],
  },
  darkMode: false,
  variants: {
    extend: {},
  },
  plugins: [],
}
