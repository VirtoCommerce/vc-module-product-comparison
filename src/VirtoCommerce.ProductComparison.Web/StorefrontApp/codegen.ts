import { CODEGEN_CONFIG, CODEGEN_PLUGINS } from "@vc-frontend/core/codegen";
import { loadEnv } from "vite";
import type { CodegenConfig } from "@graphql-codegen/cli";

// graphql-codegen reads no .env of its own; reuse Vite's loader so one file serves both. The
// shell wins over .env.
const env = { ...loadEnv("", process.cwd(), ""), ...process.env };

if (!env.APP_BACKEND_URL) {
  throw new Error("APP_BACKEND_URL is not set - copy .env.example to .env, or export it.");
}

const STOREFRONT_DOCUMENTS = "src/api/graphql/storefront/**/*.graphql";

const codegen: CodegenConfig = {
  // Scalars and plugins come from the host so the same backend value never gets two different
  // TypeScript types.
  generates: {
    // The module's own operations, against the schema it registers in Module.cs:
    // appBuilder.UseScopedSchema<…>("product-comparison").
    "src/api/graphql/types.ts": {
      schema: `${env.APP_BACKEND_URL}/graphql/product-comparison`,
      documents: ["src/api/graphql/**/*.graphql", `!${STOREFRONT_DOCUMENTS}`],
      plugins: CODEGEN_PLUGINS,
      config: CODEGEN_CONFIG,
    },
    // The storefront operation the compare page also runs: the configured price.
    "src/api/graphql/storefront/types.ts": {
      schema: `${env.APP_BACKEND_URL}/graphql`,
      documents: STOREFRONT_DOCUMENTS,
      plugins: CODEGEN_PLUGINS,
      config: { ...CODEGEN_CONFIG, onlyOperationTypes: true },
    },
  },
};

export default codegen;
