/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./Pages/**/*.cshtml",
        "./Views/**/*.cshtml",
        "./Components/**/*.cshtml"
    ],
    theme: {
        extend: {
            fontFamily: {
                poppins: ['"Poppins"', 'serif'],
                satisfy: ['"Satisfy"', 'serif'],
            },
        },
    },
    plugins: [require("daisyui")],
    daisyui: {
        themes: [
            {
                habitstheme: {
                    primary: "#5ee9b5",
                    secondary: "#fbcfe8",
                    accent: "#5ee9b5",
                    neutral: "#cbffff",
                    "base-100": "#2a2e38",
                    "base-200": "#4a5662",
                    info: "#f9a8d4",
                    success: "#d8b4fe",
                    warning: "#fda4af",
                    error: "#ffbab9",
                }
            },
        ],
    },
};