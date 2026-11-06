# Unreleased facade

`vc-frontend-core-0.2.8.tgz` is `@vc-frontend/core` 0.2.8 packed from the vc-frontend pull request that adds what this
plugin uses: the `productActions` extension point, the count on header links, `registerProductConfigurationSource`
and `useShortCart`. The facade is released only from vc-frontend `dev`, so until that pull request merges and
`core-v0.2.8` is released, the plugin installs this copy.

When `core-v0.2.8` is released, pin
`https://github.com/VirtoCommerce/vc-frontend/releases/download/core-v0.2.8/vc-frontend-core-0.2.8.tgz` in
`package.json`, delete this folder and run `yarn install`, before this module's pull request merges.
