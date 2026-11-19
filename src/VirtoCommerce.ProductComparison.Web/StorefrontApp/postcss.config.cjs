const path = require("path");

// Same pipeline as the host, with Tailwind pinned to THIS plugin's config, for `@apply` in the components' style blocks.
module.exports = {
  plugins: {
    "postcss-import": {},
    "tailwindcss/nesting": {},
    tailwindcss: { config: path.resolve(__dirname, "tailwind.config.cjs") },
    autoprefixer: {},
  },
};
