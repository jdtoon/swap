/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.cshtml",
    "./Views/**/*.cshtml",
    "./Components/**/*.cshtml"
  ],
  theme: {
    extend: {},
  },
  plugins: [require("daisyui")],
  daisyui: {
    themes: ["cupcake"], // Same theme as TTW for consistency
  },
}
