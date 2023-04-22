const colors = require('tailwindcss/colors');

/** @type {import('tailwindcss').Config} */
module.exports = {
  purge: {
    enabled: true,
    content: [
      "./src/**/*.{js,jsx,ts,tsx}",
    ],
  },
  darkMode: false,
  variants: {
    extend: {},
  },
  plugins: [],
}
