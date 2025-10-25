/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Pages/**/*.cshtml",
    "./Views/**/*.cshtml",
    "./Components/**/*.cshtml",
  ],
  theme: {
    extend: {
      fontFamily: {
        lumios: ['"Lumios Marker"', "cursive"],
        poppins: ['"Poppins"', "sans-serif"],
        poppinsMedium: ['"Poppins Medium"', "sans-serif"],
        glacial: ['"Glacial Indifference"', "sans-serif"],
      },
    },
  },
  plugins: [require("daisyui")],
  daisyui: {
    themes: ["cupcake"],
  },
};
